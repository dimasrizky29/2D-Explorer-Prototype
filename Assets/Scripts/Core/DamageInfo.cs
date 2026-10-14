using UnityEngine;

public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly Vector2 Source;
    public readonly float Knockback;
    public readonly HitFeel Feel;   // null = feel bawaan (HitFeel.Default)

    public DamageInfo(int amount, Vector2 source, float knockback = 0f, HitFeel feel = null)
    {
        Amount = amount;
        Source = source;
        Knockback = knockback;
        Feel = feel;
    }
}
