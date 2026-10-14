using NUnit.Framework;
using UnityEngine;

public sealed class ShakeCurveTests
{
    private static readonly Vector2 Right = Vector2.right;
    private static readonly Vector2 Noise = new Vector2(0.6f, -0.8f);

    [Test]
    public void FullyDirectionalStartsAsAKickAlongTheHit()
    {
        Vector2 offset = ShakeCurve.Evaluate(Right, Noise, 0.2f, 1f, 0f);

        Assert.AreEqual(0.2f, offset.x, 0.0001f);
        Assert.AreEqual(0f, offset.y, 0.0001f);
    }

    [Test]
    public void DirectionalShakeRecoilsAgainstTheHitLater()
    {
        Vector2 offset = ShakeCurve.Evaluate(Right, Noise, 0.2f, 1f, 0.25f);

        Assert.Less(offset.x, 0f);
        Assert.AreEqual(0f, offset.y, 0.0001f);
    }

    [Test]
    public void NonDirectionalIsPlainDecayingNoise()
    {
        Vector2 offset = ShakeCurve.Evaluate(Right, Noise, 0.2f, 0f, 0.5f);

        Assert.AreEqual(Noise.x * 0.2f * 0.5f, offset.x, 0.0001f);
        Assert.AreEqual(Noise.y * 0.2f * 0.5f, offset.y, 0.0001f);
    }

    [Test]
    public void EndsAtZeroAndStaysThereAfterwards()
    {
        Assert.AreEqual(Vector2.zero, ShakeCurve.Evaluate(Right, Noise, 0.2f, 0.8f, 1f));
        Assert.AreEqual(Vector2.zero, ShakeCurve.Evaluate(Right, Noise, 0.2f, 0.8f, 1.5f));
    }

    [Test]
    public void StrengthFallsOffOverTime()
    {
        float early = ShakeCurve.Evaluate(Right, Vector2.zero, 0.2f, 1f, 0f).magnitude;
        float late = ShakeCurve.Evaluate(Right, Vector2.zero, 0.2f, 1f, 0.5f).magnitude;

        Assert.Greater(early, late);
    }
}
