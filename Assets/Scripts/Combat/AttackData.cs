using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Attack Data", fileName = "AttackData")]
public sealed class AttackData : ScriptableObject
{
    [SerializeField] private int attackIndex;
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitRadius = 0.6f;
    [SerializeField] private float hitOffset = 0.5f;
    [SerializeField] private float knockback = 0f;   // 0 = tanpa dorongan
    [SerializeField] private HitFeel feel = new HitFeel();
    [SerializeField] private float windup = 0.15f;
    [SerializeField] private float recovery = 0.35f;
    [SerializeField] private LayerMask targetMask;   // 0 = pakai defaultTargetMask dari MeleeDamageDealer

    public int AttackIndex => attackIndex;
    public int Damage => damage;
    public float HitRadius => hitRadius;
    public float HitOffset => hitOffset;
    public float Knockback => knockback;
    public HitFeel Feel => feel;
    public float Windup => windup;
    public float Recovery => recovery;
    public LayerMask TargetMask => targetMask;
}
