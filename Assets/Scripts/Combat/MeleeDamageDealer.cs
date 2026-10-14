using UnityEngine;

/// <summary>
/// Satu-satunya query overlap physics di project ini. Dipakai player dan enemy.
/// </summary>
public sealed class MeleeDamageDealer : MonoBehaviour
{
    [SerializeField] private LayerMask defaultTargetMask;

    private readonly Collider2D[] _hits = new Collider2D[8];
    private readonly IDamageable[] _dealtTo = new IDamageable[8];
    private ContactFilter2D _filter;

    private void Awake()
    {
        _filter = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = defaultTargetMask
        };
    }

    /// <summary>Pusat query overlap. Dipakai ApplyHit dan DrawGizmo supaya keduanya tidak pernah beda.</summary>
    public static Vector2 ResolveCenter(Vector2 origin, Vector2 direction, AttackData attack)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        return origin + dir * attack.HitOffset;
    }

    /// <summary>Gambar radius + offset satu attack di Scene view. Titik asal = origin.</summary>
    public static void DrawGizmo(AttackData attack, Vector2 origin, Vector2 direction)
    {
        if (attack == null) return;

        Vector2 center = ResolveCenter(origin, direction, attack);

        Gizmos.color = new Color(1f, 0.45f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(center, attack.HitRadius);   // area hit
        Gizmos.DrawLine(origin, center);                   // offset dari titik asal
        Gizmos.DrawWireSphere(origin, 0.06f);              // titik asal
    }

    /// <summary>Layer target satu attack. Kosong = pakai defaultTargetMask.</summary>
    public LayerMask ResolveMask(AttackData attack) => attack.TargetMask.value != 0 ? attack.TargetMask : defaultTargetMask;

    public int ApplyHit(AttackData attack, Vector2 origin, Vector2 direction)
    {
        Vector2 center = ResolveCenter(origin, direction, attack);

        _filter.layerMask = ResolveMask(attack);

        int count = Physics2D.OverlapCircle(center, attack.HitRadius, _filter, _hits);

        int dealt = 0;
        for (int i = 0; i < count; i++)
        {
            if (!_hits[i].TryGetComponent(out IDamageable target)) continue;
            if (!target.IsAlive) continue;

            // Satu target bisa punya beberapa collider (badan + trigger interaksi): damage sekali saja.
            bool seen = false;
            for (int j = 0; j < dealt; j++)
                if (ReferenceEquals(_dealtTo[j], target)) { seen = true; break; }
            if (seen) continue;

            _dealtTo[dealt] = target;
            target.TakeDamage(new DamageInfo(attack.Damage, origin, attack.Knockback, attack.Feel));
            dealt++;
        }
        return dealt;
    }
}
