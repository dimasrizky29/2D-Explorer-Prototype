using UnityEngine;

/// <summary>
/// Dorong entity menjauh dari sumber damage selagi terkunci sebentar.
/// Controller yang bergerak (PlayerController / EnemyBrain) harus cek <see cref="IsActive"/>
/// dan berhenti menulis velocity selama itu, kalau tidak dorongannya langsung ditimpa.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public sealed class KnockbackReceiver : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private float duration = 0.12f;

    private float _until;
    private Collider2D _bodyCollider;

    public bool IsActive => Time.time < _until;

    /// <summary>Super armor: damage tetap masuk, dorongan diabaikan.</summary>
    public bool Immovable { get; set; }

    private void Awake()
    {
        if (health == null) health = GetComponent<Health>();
        if (body == null) body = GetComponent<Rigidbody2D>();

        // Collider badan = collider solid pertama di root (trigger interaksi dilewati).
        foreach (var c in GetComponents<Collider2D>())
        {
            if (c.isTrigger) continue;
            _bodyCollider = c;
            break;
        }
    }

    private void OnEnable()
    {
        if (health != null) health.Damaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (health != null) health.Damaged -= HandleDamaged;
    }

    private void HandleDamaged(DamageInfo info)
    {
        if (body == null || Immovable || info.Knockback <= 0f) return;

        // Dari pusat badan, bukan pivot kaki: Source juga setinggi badan, jadi serangan lurus tetap horizontal.
        Vector2 center = _bodyCollider != null ? (Vector2)_bodyCollider.bounds.center : (Vector2)transform.position;
        Vector2 away = center - info.Source;
        if (away.sqrMagnitude < 0.0001f) away = Vector2.up;   // sumber persis di posisi ini

        float push = info.Feel != null && info.Feel.KnockbackDuration > 0f ? info.Feel.KnockbackDuration : duration;
        _until = Time.time + push;
        body.linearVelocity = away.normalized * info.Knockback;
        body.WakeUp();
    }
}
