using UnityEngine;
using VContainer;
//using YatechStudio.Audio;

[RequireComponent(typeof(Animator))]
public class PlayerAnimator : MonoBehaviour
{
    private Animator _animator;
    public FloatingJoystick _joystick;
    public PlayerInteractor _playerInteractor;
    public PlayerController _playerController;
    [SerializeField] private PlayerFacing _facing;

    private ActionGate _gate;

    [Inject]
    public void Construct(ActionGate gate)
    {
        _gate = gate;
    }

    private static readonly int MoveX = AnimatorParams.MoveX;
    private static readonly int MoveY = AnimatorParams.MoveY;
    private static readonly int Speed = AnimatorParams.Speed;

    [SerializeField] private float speedDampTime = 0.1f;

    //private IAudioService _audioService;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (_playerInteractor != null)
        {
            _playerInteractor.OnInteract += HandleInteract;
        }

        //_audioService = GlobalResolver.Instance.Resolve<IAudioService>();
    }

    private void Update()
    {
        if (_playerController == null) return;

        // Attack / dash menguasai animasi sampai gate dilepas.
        if (_gate != null && _gate.IsBusy)
        {
            _animator.SetFloat(Speed, 0f);
            return;
        }

        // Selagi terdorong, velocity berasal dari knockback, bukan jalan: Speed dipaksa 0 dan arah hadap dibiarkan.
        if (_playerController.IsKnockedBack)
        {
            _animator.SetFloat(Speed, 0f);
            return;
        }

        Vector2 actualVelocity = _playerController != null ? _playerController.CurrentVelocity : Vector2.zero;
        float actualSpeed = actualVelocity.magnitude;

        // Normalisasi kecepatan ke rentang 0..1 berdasarkan Max MoveSpeed karakter
        float targetSpeedNormalized = Mathf.Clamp01(actualSpeed / _playerController.MoveSpeed);

        // DampTime akan membuat perubahan angka Speed terasa sangat mulus dan transisi animasi tidak kaku!
        _animator.SetFloat(Speed, targetSpeedNormalized);

        Vector2 inputDir = _joystick != null ? _joystick.Direction : Vector2.zero;

        // Tetap gunakan arah joystick / velocity untuk mengupdate arah hadap (Facing direction)
        Vector2 facingDir = actualSpeed > 0.05f ? actualVelocity.normalized : inputDir;
        if (facingDir.magnitude > 0.05f)
        {
            _animator.SetFloat(MoveX, facingDir.x);
            _animator.SetFloat(MoveY, facingDir.y);
            if (_facing != null) _facing.Set(facingDir);
        }
    }

    private void HandleInteract(IInteractable interactable)
    {
        if (interactable != null)
        {
            if (interactable.Label.Contains("Talk"))
                return;

            _animator.SetTrigger("Interact");
        }
    }

    //public void SoundFootStep()
    //{
    //    var random = Random.Range(1, 4);
    //    _audioService.PlayOneShot($"Step{random}");
    //}
}
