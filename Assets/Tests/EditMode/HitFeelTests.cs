using NUnit.Framework;
using UnityEngine;

public sealed class HitFeelTests
{
    [Test]
    public void DefaultMatchesLegacyHitBehaviour()
    {
        HitFeel feel = HitFeel.Default;

        Assert.AreEqual(0.1f, feel.HitStop);
        Assert.AreEqual(0.15f, feel.ShakeDuration);
        Assert.AreEqual(0.12f, feel.ShakeMagnitude);
        Assert.AreEqual(1f, feel.SlowScale);
        Assert.AreEqual(0f, feel.ZoomAmount);
    }

    [Test]
    public void DamageInfoWithoutFeelHasNone()
    {
        Assert.IsNull(new DamageInfo(1, Vector2.zero).Feel);
    }

    [Test]
    public void DamageInfoCarriesFeel()
    {
        var info = new DamageInfo(1, Vector2.zero, 0f, HitFeel.Default);

        Assert.AreSame(HitFeel.Default, info.Feel);
    }
}
