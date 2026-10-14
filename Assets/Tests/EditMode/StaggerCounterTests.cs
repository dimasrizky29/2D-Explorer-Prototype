using NUnit.Framework;

public sealed class StaggerCounterTests
{
    private static bool[] Run(StaggerCounter counter, int calls)
    {
        var results = new bool[calls];
        for (int i = 0; i < calls; i++) results[i] = counter.ShouldCancel();
        return results;
    }

    [Test]
    public void FixedRollCancelsThenLetsOneThrough()
    {
        var counter = new StaggerCounter(2, 2, (min, max) => 2);

        Assert.AreEqual(new[] { true, true, false, true, true, false }, Run(counter, 6));
    }

    [Test]
    public void RerollsAfterEachBreakthrough()
    {
        int roll = 0;
        var counter = new StaggerCounter(2, 3, (min, max) => roll++ % 2 == 0 ? 3 : 2);

        Assert.AreEqual(new[] { true, true, true, false, true, true, false }, Run(counter, 7));
    }

    [Test]
    public void SingleHitRangeAlternates()
    {
        var counter = new StaggerCounter(1, 1, (min, max) => 1);

        Assert.AreEqual(new[] { true, false, true, false }, Run(counter, 4));
    }

    [Test]
    public void RollReceivesTheConfiguredRange()
    {
        int seenMin = -1, seenMax = -1;
        new StaggerCounter(2, 3, (min, max) => { seenMin = min; seenMax = max; return 2; });

        Assert.AreEqual(2, seenMin);
        Assert.AreEqual(3, seenMax);
    }
}
