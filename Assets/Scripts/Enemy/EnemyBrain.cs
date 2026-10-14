using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

public enum EnemyState { Idle, Chase, Circle, Attack, Dead }

/// <summary>
/// Chase = pegang token serang, menuju slot di sekeliling player.
/// Circle = tidak dapat giliran (atau masih cooldown): mengitari di ring tunggu.
/// </summary>
public sealed class EnemyBrain : MonoBehaviour
{
    [SerializeField] private EnemyProfile profile;
    [SerializeField] private EnemySteering steering;
    [SerializeField] private MeleeDamageDealer dealer;
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Animator animator;
    [SerializeField] private KnockbackReceiver knockback;
    [SerializeField] private Vector3 posOffset;

    private const float SlotRadiusFactor = 0.7f;   // slot ada di dalam attackRange, supaya sampai di slot = sudah bisa memukul
    private const float ChaseTimeout = 4f;         // token dikembalikan kalau tidak kunjung sampai (macet di props)

    private Transform _target;
    private AttackCoordinator _coordinator;
    private Collider2D _targetBody;
    private EnemyState _state;
    private CancellationTokenSource _loopCts;
    private float _nextAttackTime;
    private float _chaseDeadline;
    private float _strafeSign = 1f;
    private Vector2 _lastDir = Vector2.right;
    private bool _isDead;   // dicek di awal loop: state bisa tertimpa (Attack menulis Circle setelah await)
    private StaggerCounter _stagger;
    private CancellationTokenSource _attackCts;   // hidup selama satu serangan; di-cancel kalau serangan dibatalkan
    private bool _hitDelivered;                   // setelah ApplyHit serangan tidak bisa ditarik

    public Vector2 Origin => (Vector2)transform.position + (Vector2)posOffset;
    public bool IsAggressive => profile != null && profile.Aggressive;
    public bool BypassesTurns => profile != null && profile.BypassTurns;
    public bool ReadyToAttack => _state != EnemyState.Dead && Time.time >= _nextAttackTime;
    public bool IsEngaged => _state != EnemyState.Idle && _state != EnemyState.Dead;

    /// <summary>Boleh diambil alih (BossBrain): sedang mengejar/mengitari, tidak di tengah attack atau terdorong.</summary>
    public bool CanBeInterrupted =>
        (_state == EnemyState.Chase || _state == EnemyState.Circle) && (knockback == null || !knockback.IsActive);

    /// <summary>Selagi true, loop berhenti menulis velocity; pemegang (BossBrain) yang mengendalikan.</summary>
    public bool Suspended { get; set; }

    public float SpeedMultiplier { get; set; } = 1f;

    /// <summary>Dipancarkan tiap attack biasa selesai mengenai/meleset.</summary>
    public event Action Attacked;

    // Pusat badan target (bukan pivot kaki), dipakai untuk jarak serang + arah hit.
    public Vector2 TargetCenter => _targetBody != null ? (Vector2)_targetBody.bounds.center : (Vector2)_target.position;

    [Inject]
    public void Construct(PlayerFacing player, AttackCoordinator coordinator)
    {
        _target = player.transform;
        _coordinator = coordinator;

        foreach (var c in player.GetComponents<Collider2D>())
        {
            if (c.isTrigger) continue;
            _targetBody = c;
            break;
        }
    }

    private void Awake()
    {
        if (body == null) body = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (health == null) health = GetComponent<Health>();
        if (knockback == null) knockback = GetComponent<KnockbackReceiver>();
        if (steering == null) steering = GetComponent<EnemySteering>();
        _strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
    }

    private void OnEnable()
    {
        if (profile == null)
        {
            Debug.LogError("[EnemyBrain] EnemyProfile belum diisi.", this);
            return;
        }

        _stagger = new StaggerCounter(profile.StaggerMinHits, profile.StaggerMaxHits,
            (min, max) => UnityEngine.Random.Range(min, max + 1));
        if (health != null)
        {
            health.Died += HandleDied;
            health.Damaged += HandleStagger;
        }

        // CTS sendiri, bukan destroyCancellationToken: loop harus ikut mati saat
        // komponen dinonaktifkan, dan tidak boleh menumpuk kalau di-enable berkali-kali.
        _loopCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
        RunLoop(_loopCts.Token).Forget();
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= HandleDied;
            health.Damaged -= HandleStagger;
        }

        // Token tidak boleh bocor kalau enemy hilang di tengah giliran.
        _coordinator?.Disengage(this);

        if (_loopCts != null)
        {
            _loopCts.Cancel();
            _loopCts.Dispose();
            _loopCts = null;
        }
    }

    private void HandleDied()
    {
        animator.SetTrigger(AnimatorParams.IsDead);
        _state = EnemyState.Dead;
        _isDead = true;
        // Dorongan dari hit fatal dibiarkan jalan supaya pukulan penutup terasa melontarkan; selain itu diam.
        if (knockback == null || !knockback.IsActive) body.linearVelocity = Vector2.zero;
        _coordinator?.Disengage(this);
    }

    /// <summary>
    /// Hit player membatalkan niat menyerang (menuju slot atau windup) selama hitungan stagger masih ada.
    /// Selagi Suspended (boss Charge) dan setelah ApplyHit, hit tidak berpengaruh dan hitungan tidak berkurang.
    /// Hanya player yang melukai enemy (layer mask), jadi sumber damage tidak dicek.
    /// </summary>
    private void HandleStagger(DamageInfo info)
    {
        if (_isDead || Suspended || _stagger == null) return;

        bool attacking = _state == EnemyState.Attack && _attackCts != null && !_hitDelivered;
        if (!attacking && _state != EnemyState.Chase) return;
        if (!_stagger.ShouldCancel()) return;

        if (attacking)
        {
            _attackCts.Cancel();   // RunAttack yang mengurus cooldown dan state
            return;
        }

        _coordinator?.Release(this);
        _nextAttackTime = Time.time + profile.StaggerCooldown;
        _state = EnemyState.Circle;
    }

    private void OnDrawGizmos()
    {
        if (profile == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(Origin, profile.AttackRange);   // mulai menyerang
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Origin, profile.DetectRadius);

        if (profile.Attack == null) return;

        Vector2 dir = _target != null ? (TargetCenter - Origin).normalized : (Vector2)transform.right;
        MeleeDamageDealer.DrawGizmo(profile.Attack, Origin, dir);
    }

    private float DistanceToTarget()
    {
        if (_target == null) return float.MaxValue;
        return Vector2.Distance(Origin, TargetCenter);
    }

    // Player bisa menembus enemy: kalau posisinya bertumpuk, arah normalize jadi nol. Pakai arah terakhir.
    public Vector2 DirToTarget()
    {
        Vector2 d = TargetCenter - Origin;
        if (d.sqrMagnitude > 0.0001f) _lastDir = d.normalized;
        return _lastDir;
    }

    private void Move(Vector2 desired)
    {
        Vector2 steered = steering != null ? steering.Steer(desired) : desired;
        body.linearVelocity = steered * (profile.WalkSpeed * SpeedMultiplier);

        // Hadap ke player walau sedang mengitari; Speed = porsi kecepatan 0..1.
        Vector2 face = DirToTarget();
        animator.SetFloat(AnimatorParams.Speed, steered.magnitude);
        animator.SetFloat(AnimatorParams.MoveX, face.x);
        animator.SetFloat(AnimatorParams.MoveY, face.y);
    }

    private void Stop()
    {
        body.linearVelocity = Vector2.zero;
        animator.SetFloat(AnimatorParams.Speed, 0f);
    }

    /// <summary>Tentukan Chase (dapat giliran) atau Circle (menunggu). Dipanggil tiap tick selagi engaged.</summary>
    private void Decide()
    {
        if (ReadyToAttack && _coordinator.TryAcquire(this))
        {
            if (_state != EnemyState.Chase) _chaseDeadline = Time.time + ChaseTimeout;
            _state = EnemyState.Chase;
        }
        else
        {
            _state = EnemyState.Circle;
        }
    }

    private async UniTaskVoid RunLoop(CancellationToken ct)
    {
        try
        {
            // Enemy hasil spawn di-inject tepat setelah Instantiate; beri satu frame sebelum cek.
            await UniTask.Yield(ct);
            if (_coordinator == null)
            {
                Debug.LogError("[EnemyBrain] Belum di-inject. Spawn lewat IObjectResolver.Instantiate.", this);
                return;
            }

            while (!ct.IsCancellationRequested)
            {
                if (_isDead)
                {
                    // Mayat meluncur sampai dorongan hit fatal habis, baru diam.
                    while (knockback != null && knockback.IsActive)
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                    Stop();
                    return;
                }

                // Selagi terdorong, velocity dipegang KnockbackReceiver; selagi Suspended, dipegang BossBrain.
                if (Suspended || (knockback != null && knockback.IsActive))
                {
                    await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                    continue;
                }

                switch (_state)
                {
                    case EnemyState.Idle:
                        Stop();
                        if (DistanceToTarget() < profile.DetectRadius)
                        {
                            _coordinator.Engage(this);
                            Decide();
                            continue;
                        }
                        await UniTask.Delay(
                            TimeSpan.FromSeconds(UnityEngine.Random.Range(profile.IdleMinDelay, profile.IdleMaxDelay)),
                            cancellationToken: ct);
                        break;

                    case EnemyState.Chase:
                    {
                        if (_target == null || DistanceToTarget() > profile.LoseRadius) { GoIdle(); continue; }
                        if (DistanceToTarget() < profile.AttackRange) { _state = EnemyState.Attack; continue; }

                        if (Time.time > _chaseDeadline)
                        {
                            // Tidak sampai-sampai (macet): serahkan giliran, coba lagi nanti.
                            _coordinator.Release(this);
                            _nextAttackTime = Time.time + profile.AttackCooldown;
                            _state = EnemyState.Circle;
                            continue;
                        }

                        Vector2 center = TargetCenter;
                        Vector2 slot = center + _coordinator.SlotDirection(this, center) * (profile.AttackRange * SlotRadiusFactor);
                        Vector2 toSlot = slot - Origin;
                        Move(toSlot.sqrMagnitude > 0.0001f ? toSlot.normalized : Vector2.zero);
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                        break;
                    }

                    case EnemyState.Circle:
                    {
                        if (_target == null || DistanceToTarget() > profile.LoseRadius) { GoIdle(); continue; }

                        Decide();
                        if (_state != EnemyState.Circle) continue;

                        // Tarik ke ring tunggu, plus geser menyamping seporsi strafe.
                        Vector2 fromTarget = Origin - TargetCenter;
                        float dist = fromTarget.magnitude;
                        Vector2 radial = dist > 0.0001f ? fromTarget / dist : Vector2.right;
                        Vector2 tangent = new Vector2(-radial.y, radial.x) * _strafeSign;

                        float error = Mathf.Clamp(dist - profile.WaitRadius, -1f, 1f);
                        Vector2 desired = -radial * error + tangent * profile.Strafe;
                        Move(Vector2.ClampMagnitude(desired, 1f));
                        await UniTask.Yield(PlayerLoopTiming.FixedUpdate, ct);
                        break;
                    }

                    case EnemyState.Attack:
                        Stop();
                        if (profile.Attack == null || _target == null) { _coordinator.Release(this); _state = EnemyState.Circle; continue; }

                        await RunAttack(ct);
                        break;

                    case EnemyState.Dead:
                        Stop();
                        return;
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    /// <summary>
    /// Satu serangan biasa. Windup bisa dibatalkan lewat <see cref="HandleStagger"/> (token per serangan);
    /// setelah ApplyHit tidak lagi. Dibatalkan = Hurt + cooldown stagger, selesai normal = cooldown serangan.
    /// </summary>
    private async UniTask RunAttack(CancellationToken ct)
    {
        _hitDelivered = false;
        _attackCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        bool cancelled = false;
        try
        {
            animator.SetTrigger(AnimatorParams.Attack);

            await UniTask.Delay(TimeSpan.FromSeconds(profile.Attack.Windup), cancellationToken: _attackCts.Token);
            if (_isDead) return;
            dealer.ApplyHit(profile.Attack, Origin, DirToTarget());
            _hitDelivered = true;
            Attacked?.Invoke();

            float tail = profile.Attack.Recovery - profile.Attack.Windup;
            if (tail > 0f) await UniTask.Delay(TimeSpan.FromSeconds(tail), cancellationToken: ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            cancelled = true;
        }
        finally
        {
            _attackCts.Dispose();
            _attackCts = null;
        }

        if (_isDead) return;

        _coordinator.Release(this);
        if (cancelled)
        {
            animator.SetTrigger(AnimatorParams.Hurt);
            _nextAttackTime = Time.time + profile.StaggerCooldown;
        }
        else
        {
            _nextAttackTime = Time.time + profile.AttackCooldown;
        }
        _strafeSign = UnityEngine.Random.value < 0.5f ? -1f : 1f;
        _state = EnemyState.Circle;
    }

    private void GoIdle()
    {
        _coordinator.Disengage(this);
        _state = EnemyState.Idle;
    }
}
