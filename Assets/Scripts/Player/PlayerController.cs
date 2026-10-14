using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private InputActionReference moveAction;

    private Rigidbody2D _rb;
    private ActionGate _gate;
    public FloatingJoystick _joystick;
    [SerializeField] private KnockbackReceiver knockback;


    // Velocity dari knockback masih tertinggal sampai FixedUpdate berikutnya; jangan dibaca sebagai jalan.
    private bool _knockbackStale;

    public Vector2 CurrentVelocity => _rb != null && !_knockbackStale ? _rb.linearVelocity : Vector2.zero;
    public float MoveSpeed => moveSpeed;
    public bool IsKnockedBack => knockback != null && knockback.IsActive;

    [Inject]
    public void Construct(ActionGate gate)
    {
        _gate = gate;
    }

    private void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        if (knockback == null) knockback = GetComponent<KnockbackReceiver>();
        // Ensure physics settings are correct for topdown movement
        _rb.gravityScale = 0f;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void FixedUpdate()
    {
        // Saat attack / dash jalan, velocity diatur PlayerAttack / PlayerDash.
        if (_gate != null && _gate.IsBusy) return;

        // Selagi terdorong, velocity dipegang KnockbackReceiver.
        if (knockback != null && knockback.IsActive)
        {
            _knockbackStale = true;
            return;
        }
        _knockbackStale = false;

        _rb.linearVelocity = ReadMoveInput() * moveSpeed;
    }

    /// <summary>Input gerak saat ini. Keyboard prioritas; joystick jadi fallback (mobile).</summary>
    public Vector2 ReadMoveInput()
    {
        Vector2 moveDir = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        if (moveDir.sqrMagnitude < 0.01f && _joystick != null) moveDir = _joystick.Direction;
        return moveDir;
    }
}

