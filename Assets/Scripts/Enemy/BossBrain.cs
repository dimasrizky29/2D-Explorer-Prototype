using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Boss = EnemyBrain biasa + ultimate Charge. Selagi Charge, EnemyBrain di-Suspend dan
/// komponen ini yang memegang velocity. Charge muncul tiap N attack biasa atau tiap interval detik.
/// Semua jeda pakai waktu scaled supaya ikut beku saat hit stop.
/// </summary>
[RequireComponent(typeof(EnemyBrain))]
public sealed class BossBrain : MonoBehaviour
{
    [SerializeField] private EnemyBrain brain;
    [SerializeField] private MeleeDamageDealer dealer;
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;
    [SerializeField] private KnockbackReceiver knockback;
    [SerializeField] private Collider2D feet;               // dipakai cast ke props saat dash
    [SerializeField] private LineRenderer telegraph;
    [SerializeField] private LayerMask obstacleMask = 1 << 9;

    // ponytail: angka boss langsung di komponen; pindah ke ScriptableObject kalau boss sudah lebih dari satu jenis.
    [Header("Charge")]
    [SerializeField] private AttackData chargeAttack;
    [SerializeField] private int attacksPerCharge = 3;
    [SerializeField] private float chargeInterval = 12f;
    [SerializeField] private float telegraphTime = 0.8f;
    [SerializeField] private float lockTime = 0.15f;        // arah dikunci: celah player untuk menghindar
    [SerializeField] private float chargeDistance = 6f;
    [SerializeField] private float chargeSpeed = 14f;
    [SerializeField] private float recoverTime = 1.5f;
    [SerializeField] private float stunTime = 2.5f;         // menabrak props = celah besar untuk player

    [Header("Phase 2")]
    [SerializeField, Range(0f, 1f)] private float phase2Threshold = 0.5f;
    [SerializeField] private float phase2Interval = 8f;
    [SerializeField] private float phase2Telegraph = 0.6f;
    [SerializeField] private float phase2SpeedMultiplier = 1.2f;

    private readonly RaycastHit2D[] _castHits = new RaycastHit2D[4];
    private ContactFilter2D _obstacleFilter;
    private int _attacksSinceCharge;
    private float _engagedTime;
    private bool _phase2;

    public bool IsCharging { get; private set; }

    private float Interval => _phase2 ? phase2Interval : chargeInterval;
    private float Telegraph => _phase2 ? phase2Telegraph : telegraphTime;

    private void Awake()
    {
        if (brain == null) brain = GetComponent<EnemyBrain>();
        if (dealer == null) dealer = GetComponent<MeleeDamageDealer>();
        if (health == null) health = GetComponent<Health>();
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (knockback == null) knockback = GetComponent<KnockbackReceiver>();
        if (telegraph != null) telegraph.enabled = false;

        _obstacleFilter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = obstacleMask };
    }

    private void OnEnable()
    {
        if (chargeAttack == null)
        {
            Debug.LogError("[BossBrain] Charge AttackData belum diisi.", this);
            return;
        }

        brain.Attacked += HandleAttacked;
        if (health != null) health.Changed += HandleHealthChanged;
        RunLoop(destroyCancellationToken).Forget();
    }

    private void OnDisable()
    {
        brain.Attacked -= HandleAttacked;
        if (health != null) health.Changed -= HandleHealthChanged;
    }

    private void HandleAttacked() => _attacksSinceCharge++;

    private void HandleHealthChanged(int current, int max)
    {
        if (_phase2 || current > max * phase2Threshold) return;
        _phase2 = true;
        brain.SpeedMultiplier = phase2SpeedMultiplier;
    }

    private bool ChargeDue() =>
        _attacksSinceCharge >= attacksPerCharge || _engagedTime >= Interval;

    private async UniTaskVoid RunLoop(CancellationToken ct)
    {
        try
        {
            while (true)
            {
                // Yield dulu: Health mungkin belum Awake saat OnEnable komponen ini jalan.
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                if (!enabled || (health != null && !health.IsAlive)) return;

                if (!brain.IsEngaged) { _engagedTime = 0f; continue; }
                _engagedTime += Time.fixedDeltaTime;

                // Tunggu attack biasa / dorongan selesai, dan player cukup dekat untuk diterjang.
                if (!ChargeDue() || !brain.CanBeInterrupted) continue;
                if (Vector2.Distance(brain.Origin, brain.TargetCenter) > chargeDistance) continue;

                await Charge(ct);
                _attacksSinceCharge = 0;
                _engagedTime = 0f;
            }
        }
        catch (OperationCanceledException) { }
    }

    private async UniTask Charge(CancellationToken ct)
    {
        IsCharging = true;
        brain.Suspended = true;
        knockback.Immovable = true;   // super armor selama telegraph + dash
        body.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetFloat(AnimatorParams.Speed, 0f);

        try
        {
            // Telegraph: garis mengikuti player, lalu berhenti mengikuti saat dikunci.
            Vector2 dir = brain.DirToTarget();
            SetTelegraph(true, dir);
            for (float t = 0f; t < Telegraph; t += Time.fixedDeltaTime)
            {
                dir = brain.DirToTarget();
                SetTelegraph(true, dir);
                Face(dir, 0f);
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                if (!health.IsAlive) return;
            }
            await UniTask.Delay(TimeSpan.FromSeconds(lockTime), cancellationToken: ct);
            SetTelegraph(false, dir);

            bool hitPlayer = false;
            bool hitWall = false;
            float step = chargeSpeed * Time.fixedDeltaTime;
            Face(dir, 1f);
            for (float traveled = 0f; traveled < chargeDistance; traveled += step)
            {
                if (!health.IsAlive) return;

                // Cast kaki (bukan badan): sama dengan yang menahan boss di props.
                if (feet != null && feet.Cast(dir, _obstacleFilter, _castHits, step) > 0) { hitWall = true; break; }

                body.linearVelocity = dir * chargeSpeed;
                if (!hitPlayer) hitPlayer = dealer.ApplyHit(chargeAttack, brain.Origin, dir) > 0;   // damage sekali per charge
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
            }

            body.linearVelocity = Vector2.zero;
            knockback.Immovable = false;
            Face(dir, 0f);

            // Recover/stun: boss diam & bisa didorong; velocity dinolkan lagi setelah dorongan habis.
            float wait = hitWall ? stunTime : recoverTime;
            for (float t = 0f; t < wait; t += Time.fixedDeltaTime)
            {
                if (!knockback.IsActive) body.linearVelocity = Vector2.zero;
                await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                if (!health.IsAlive) return;
            }
        }
        finally
        {
            SetTelegraph(false, Vector2.zero);
            knockback.Immovable = false;
            brain.Suspended = false;
            IsCharging = false;
        }
    }

    private void Face(Vector2 dir, float speed)
    {
        if (animator == null) return;
        animator.SetFloat(AnimatorParams.Speed, speed);
        animator.SetFloat(AnimatorParams.MoveX, dir.x);
        animator.SetFloat(AnimatorParams.MoveY, dir.y);
    }

    private void SetTelegraph(bool on, Vector2 dir)
    {
        if (telegraph == null) return;
        telegraph.enabled = on;
        if (!on) return;

        Vector2 from = brain.Origin;
        telegraph.SetPosition(0, from);
        telegraph.SetPosition(1, from + dir * chargeDistance);
    }

    private void OnDrawGizmosSelected()
    {
        if (brain == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(brain.Origin, chargeDistance);   // jangkauan charge
    }
}
