using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public sealed class PlayerAttack : MonoBehaviour
{
    [SerializeField] private ComboData combo;               // urutan serangan; langkah terakhir = finisher
    [SerializeField] private MeleeDamageDealer dealer;
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerFacing facing;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private PlayerController controller;

    [SerializeField] private Vector3 posOffsetPlayer;

    [SerializeField] private float lockRange = 1f;                          // jarak cari enemy untuk auto lock
    [SerializeField, Range(0f, 90f)] private float maxLockAngle = 30f;      // batas belok saat joystick dipegang

    private ActionGate _gate;
    private readonly Collider2D[] _lockHits = new Collider2D[8];
    private ContactFilter2D _filter;
    private ComboTracker _tracker;
    private float _bufferUntil = -1f;   // tombol yang ditekan selagi sibuk, berlaku sampai waktu ini

    [Inject]
    public void Construct(ActionGate gate)
    {
        _gate = gate;
    }

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (facing == null) facing = GetComponent<PlayerFacing>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (controller == null) controller = GetComponent<PlayerController>();
        _filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
        if (combo != null) _tracker = new ComboTracker(combo.Length);
    }

    private void OnDrawGizmos()
    {
        if (combo == null || combo.Steps == null) return;

        Vector2 dir = facing != null ? facing.Direction : (Vector2)transform.right;
        for (int i = 0; i < combo.Steps.Length; i++)
            MeleeDamageDealer.DrawGizmo(combo.Steps[i], transform.position + posOffsetPlayer, dir);
    }

    public void TryAttack()
    {
        if (_gate == null)
        {
            Debug.LogError("[PlayerAttack] ActionGate belum di-inject. Pastikan GameLifetimeScope ada di scene.");
            return;
        }
        if (combo == null || combo.Length == 0 || dealer == null) return;
        if (_gate.IsBusy)
        {
            // Waktu scaled: hit stop tidak memakan masa berlaku buffer.
            _bufferUntil = Time.time + combo.InputBuffer;
            return;
        }

        RunAsync(destroyCancellationToken).Forget();
    }

    private async UniTaskVoid RunAsync(CancellationToken ct)
    {
        _gate.IsBusy = true;
        if (body != null) body.linearVelocity = Vector2.zero;   // stop lari saat attack
        try
        {
            AttackData attack = combo.Steps[_tracker.Begin(Time.time)];
            animator.SetInteger(AnimatorParams.AttackIndex, attack.AttackIndex);
            animator.SetTrigger(AnimatorParams.Attack);

            // Hadap ke target sejak awal windup, lalu arah dihitung ulang saat hit (enemy bisa bergerak).
            FaceTo(ResolveAttackDir(attack));

            await UniTask.Delay(TimeSpan.FromSeconds(attack.Windup), cancellationToken: ct);
            Vector2 hitDir = ResolveAttackDir(attack);
            FaceTo(hitDir);
            int dealt = dealer.ApplyHit(attack, transform.position + posOffsetPlayer, hitDir);
            if (dealt > 0) _tracker.OnHit(Time.time, combo.ComboWindow);
            else _tracker.OnMiss();

            float tail = attack.Recovery - attack.Windup;
            if (tail > 0f) await UniTask.Delay(TimeSpan.FromSeconds(tail), cancellationToken: ct);
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (_gate != null) _gate.IsBusy = false;
        }

        // Tombol yang ditekan sebelum serangan ini selesai langsung lanjut ke langkah berikutnya.
        if (!ct.IsCancellationRequested && _bufferUntil > Time.time)
        {
            _bufferUntil = -1f;
            TryAttack();
        }
    }

    /// <summary>Set arah hadap logis dan parameter MoveX/MoveY, karena visual hadap dibaca dari situ.</summary>
    private void FaceTo(Vector2 dir)
    {
        facing.Set(dir);
        if (dir.sqrMagnitude < 0.0001f) return;
        Vector2 n = dir.normalized;
        animator.SetFloat(AnimatorParams.MoveX, n.x);
        animator.SetFloat(AnimatorParams.MoveY, n.y);
    }

    /// <summary>
    /// Joystick diam: hadap enemy terdekat dalam lockRange. Joystick dipegang: belok ke enemy hanya jika selisih sudut ≤ maxLockAngle.
    /// Tanpa enemy: arah input / facing biasa.
    /// </summary>
    private Vector2 ResolveAttackDir(AttackData attack)
    {
        Vector2 origin = transform.position + posOffsetPlayer;
        Vector2 input = controller != null ? controller.ReadMoveInput() : Vector2.zero;
        bool hasInput = input.sqrMagnitude >= 0.01f;
        Vector2 baseDir = hasInput ? input.normalized : facing.Direction;

        Transform target = FindNearestEnemy(attack, origin);
        if (target == null) return baseDir;

        Vector2 toEnemy = ((Vector2)target.position - origin).normalized;
        if (!hasInput) return toEnemy;
        return Vector2.Angle(baseDir, toEnemy) <= maxLockAngle ? toEnemy : baseDir;
    }

    private Transform FindNearestEnemy(AttackData attack, Vector2 origin)
    {
        _filter.layerMask = dealer.ResolveMask(attack);
        int count = Physics2D.OverlapCircle(origin, lockRange, _filter, _lockHits);

        Transform best = null;
        float bestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            if (!_lockHits[i].TryGetComponent(out IDamageable target) || !target.IsAlive) continue;

            float sqr = ((Vector2)_lockHits[i].transform.position - origin).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = _lockHits[i].transform;
            }
        }
        return best;
    }
}
