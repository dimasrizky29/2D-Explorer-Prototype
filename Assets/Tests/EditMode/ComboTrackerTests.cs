using NUnit.Framework;

public sealed class ComboTrackerTests
{
    private const float Window = 0.6f;

    private static ComboTracker NewTracker() => new ComboTracker(5);

    [Test]
    public void FiveLandedHitsWalkTheWholeChainThenWrap()
    {
        var tracker = NewTracker();
        float now = 0f;

        for (int expected = 0; expected < 5; expected++)
        {
            Assert.AreEqual(expected, tracker.Begin(now));
            tracker.OnHit(now, Window);
            now += 0.4f;
        }

        Assert.AreEqual(0, tracker.Begin(now));
    }

    [Test]
    public void OnlyTheLastStepIsTheFinisher()
    {
        var tracker = NewTracker();

        Assert.IsTrue(tracker.IsFinisher(4));
        Assert.IsFalse(tracker.IsFinisher(3));
        Assert.IsFalse(tracker.IsFinisher(0));
    }

    [Test]
    public void PressingAfterTheWindowResetsToTheFirstHit()
    {
        var tracker = NewTracker();
        tracker.Begin(0f);
        tracker.OnHit(0.1f, Window);

        Assert.AreEqual(0, tracker.Begin(0.8f));
    }

    [Test]
    public void AMissResetsTheChain()
    {
        var tracker = NewTracker();
        tracker.Begin(0f);
        tracker.OnHit(0f, Window);
        Assert.AreEqual(1, tracker.Begin(0.1f));

        tracker.OnMiss();

        Assert.AreEqual(0, tracker.Begin(0.2f));
    }

    [Test]
    public void AnAttackThatNeverReportsCountsAsAMiss()
    {
        var tracker = NewTracker();
        tracker.Begin(0f);
        tracker.OnHit(0f, Window);
        Assert.AreEqual(1, tracker.Begin(0.1f));   // dibatalkan: tidak ada OnHit/OnMiss

        Assert.AreEqual(0, tracker.Begin(0.2f));
    }

    [Test]
    public void SpammingWithoutTargetsNeverReachesTheFinisher()
    {
        var tracker = NewTracker();

        for (int i = 0; i < 10; i++)
        {
            Assert.AreEqual(0, tracker.Begin(i * 0.3f));
            tracker.OnMiss();
        }
    }
}
