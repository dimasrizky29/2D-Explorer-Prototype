public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(in DamageInfo info);
}
