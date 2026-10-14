using NUnit.Framework;

public sealed class ZoomCurveTests
{
    private const float Amount = 0.2f;

    [Test]
    public void StartsAtZero()
    {
        Assert.AreEqual(0f, ZoomCurve.Evaluate(Amount, 0f));
    }

    [Test]
    public void PeaksAtFullAmountEarly()
    {
        Assert.AreEqual(Amount, ZoomCurve.Evaluate(Amount, 0.15f), 0.001f);
    }

    [Test]
    public void ReturnsToZeroAtTheEnd()
    {
        Assert.AreEqual(0f, ZoomCurve.Evaluate(Amount, 1f));
    }

    [Test]
    public void EasesBackDownAfterThePeak()
    {
        float mid = ZoomCurve.Evaluate(Amount, 0.575f);

        Assert.Greater(mid, 0f);
        Assert.Less(mid, Amount);
    }

    [Test]
    public void OutsideTheDurationIsZero()
    {
        Assert.AreEqual(0f, ZoomCurve.Evaluate(Amount, 1.5f));
        Assert.AreEqual(0f, ZoomCurve.Evaluate(Amount, -0.5f));
    }

    [Test]
    public void NegativeAmountZoomsOut()
    {
        Assert.Less(ZoomCurve.Evaluate(-Amount, 0.15f), 0f);
    }
}
