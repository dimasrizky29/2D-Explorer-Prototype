using NUnit.Framework;

public sealed class TimeScaleStackTests
{
    [Test]
    public void EmptyStackIsFullSpeed()
    {
        Assert.AreEqual(1f, new TimeScaleStack().Evaluate(0f));
    }

    [Test]
    public void EntryAppliesUntilItsEndExclusive()
    {
        var stack = new TimeScaleStack();
        stack.Add(0f, 1f);

        Assert.AreEqual(0f, stack.Evaluate(0.5f));
        Assert.AreEqual(1f, stack.Evaluate(1f));
    }

    [Test]
    public void OverlappingEntriesUseTheSmallestScale()
    {
        var stack = new TimeScaleStack();
        stack.Add(0f, 1f);
        stack.Add(0.25f, 2f);

        Assert.AreEqual(0f, stack.Evaluate(0.5f));
        Assert.AreEqual(0.25f, stack.Evaluate(1.5f));
        Assert.AreEqual(1f, stack.Evaluate(2f));
    }

    [Test]
    public void SameScaleExtendsInsteadOfStacking()
    {
        var stack = new TimeScaleStack();
        stack.Add(0f, 1f);
        stack.Add(0f, 1.5f);

        Assert.AreEqual(0f, stack.Evaluate(1.2f));
    }

    [Test]
    public void ShorterSameScaleDoesNotShrinkTheEnd()
    {
        var stack = new TimeScaleStack();
        stack.Add(0f, 2f);
        stack.Add(0f, 1f);

        Assert.AreEqual(0f, stack.Evaluate(1.5f));
    }

    [Test]
    public void ExpiredEntriesAreDropped()
    {
        var stack = new TimeScaleStack();
        stack.Add(0f, 1f);
        Assert.IsTrue(stack.HasEntries);

        stack.Evaluate(1f);

        Assert.IsFalse(stack.HasEntries);
    }
}
