/// <summary>
/// Logic health murni (tanpa UnityEngine) supaya bisa di-unit-test.
/// </summary>
public sealed class HealthModel
{
    public int Max { get; }
    public int Current { get; private set; }
    public bool IsAlive => Current > 0;
    public bool Invulnerable { get; set; }

    public HealthModel(int max)
    {
        Max = max < 1 ? 1 : max;
        Current = Max;
    }

    /// <returns>true kalau damage diterima.</returns>
    public bool Apply(in DamageInfo info)
    {
        if (!IsAlive || Invulnerable || info.Amount <= 0) return false;

        int next = Current - info.Amount;
        Current = next < 0 ? 0 : next;
        return true;
    }
}
