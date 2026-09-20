using System.Collections.Immutable;

namespace FunnySharp.Tests;

public sealed class ContainerBridgeTests
{
    [Fact]
    public void ContainerBridgesRejectNullSourcesEagerly()
    {
        Queue<int>? queue = null;
        Stack<int>? stack = null;
        PriorityQueue<int, int>? priorityQueue = null;
        IList<int>? list = null;
        Dictionary<string, int>? dictionary = null;

        Assert.Throws<ArgumentNullException>(() => queue!.DequeueOrNone());
        Assert.Throws<ArgumentNullException>(() => queue!.PeekOrNone());
        Assert.Throws<ArgumentNullException>(() => stack!.PopOrNone());
        Assert.Throws<ArgumentNullException>(() => stack!.PeekOrNone());
        Assert.Throws<ArgumentNullException>(() => priorityQueue!.DequeueOrNone());
        Assert.Throws<ArgumentNullException>(() => priorityQueue!.PeekOrNone());
        Assert.Throws<ArgumentNullException>(() => list!.IndexOfOrNone(1));
        Assert.Throws<ArgumentNullException>(() => dictionary!.RemoveOrNone("key"));
    }

    [Fact]
    public void QueueDequeueOrNoneReturnsTheFrontElementAndRemovesIt()
    {
        var queue = new Queue<string>();
        queue.Enqueue("first");
        queue.Enqueue("second");
        queue.Enqueue("third");

        var front = queue.DequeueOrNone();

        Assert.True(front.TryGetValue(out var value));
        Assert.Equal("first", value);
        Assert.Equal(["second", "third"], queue);

        Assert.True(queue.DequeueOrNone().TryGetValue(out var second));
        Assert.Equal("second", second);
        Assert.True(queue.DequeueOrNone().TryGetValue(out var third));
        Assert.Equal("third", third);
        Assert.True(queue.DequeueOrNone().IsNone);
        Assert.Empty(queue);
    }

    [Fact]
    public void QueuePeekOrNoneReturnsTheFrontElementWithoutRemovingIt()
    {
        var queue = new Queue<string>();
        queue.Enqueue("first");
        queue.Enqueue("second");

        var peeked = queue.PeekOrNone();

        Assert.True(peeked.TryGetValue(out var value));
        Assert.Equal("first", value);
        Assert.True(queue.PeekOrNone().TryGetValue(out var again));
        Assert.Equal("first", again);
        Assert.Equal(["first", "second"], queue);
    }

    [Fact]
    public void StackPopOrNoneReturnsTheTopElementAndRemovesIt()
    {
        var stack = new Stack<int>();
        stack.Push(1);
        stack.Push(2);
        stack.Push(3);

        var top = stack.PopOrNone();

        Assert.True(top.TryGetValue(out var value));
        Assert.Equal(3, value);
        Assert.Equal(2, stack.Count);
        Assert.DoesNotContain(3, stack);

        Assert.True(stack.PopOrNone().TryGetValue(out var second));
        Assert.Equal(2, second);
        Assert.True(stack.PopOrNone().TryGetValue(out var third));
        Assert.Equal(1, third);
        Assert.True(stack.PopOrNone().IsNone);
        Assert.Empty(stack);
    }

    [Fact]
    public void StackPeekOrNoneReturnsTheTopElementWithoutRemovingIt()
    {
        var stack = new Stack<string>();
        stack.Push("first");
        stack.Push("second");

        var peeked = stack.PeekOrNone();

        Assert.True(peeked.TryGetValue(out var value));
        Assert.Equal("second", value);
        Assert.True(stack.PeekOrNone().TryGetValue(out var again));
        Assert.Equal("second", again);
        Assert.Equal(2, stack.Count);
    }

    [Fact]
    public void PriorityQueueDequeueOrNoneReturnsTheLowestPriorityElementAndRemovesIt()
    {
        var priorityQueue = new PriorityQueue<int, int>();
        priorityQueue.Enqueue(10, 3);
        priorityQueue.Enqueue(20, 1);
        priorityQueue.Enqueue(30, 2);

        var lowest = priorityQueue.DequeueOrNone();

        Assert.True(lowest.TryGetValue(out var value));
        Assert.Equal(20, value);
        Assert.Equal(2, priorityQueue.Count);
        Assert.True(priorityQueue.PeekOrNone().TryGetValue(out var next));
        Assert.Equal(30, next);

        Assert.True(priorityQueue.DequeueOrNone().TryGetValue(out var second));
        Assert.Equal(30, second);
        Assert.True(priorityQueue.DequeueOrNone().TryGetValue(out var third));
        Assert.Equal(10, third);
        Assert.True(priorityQueue.DequeueOrNone().IsNone);
        Assert.Equal(0, priorityQueue.Count);
    }

    [Fact]
    public void PriorityQueuePeekOrNoneReturnsTheLowestPriorityElementWithoutRemovingIt()
    {
        var priorityQueue = new PriorityQueue<string, int>();
        priorityQueue.Enqueue("high", 5);
        priorityQueue.Enqueue("low", 1);

        var peeked = priorityQueue.PeekOrNone();

        Assert.True(peeked.TryGetValue(out var value));
        Assert.Equal("low", value);
        Assert.True(priorityQueue.PeekOrNone().TryGetValue(out var again));
        Assert.Equal("low", again);
        Assert.Equal(2, priorityQueue.Count);
    }

    [Fact]
    public void EmptyContainersReturnNoneWhereTheBclOperationsThrow()
    {
        var queue = new Queue<int>();
        var stack = new Stack<int>();
        var priorityQueue = new PriorityQueue<int, int>();

        Assert.True(queue.DequeueOrNone().IsNone);
        Assert.True(queue.PeekOrNone().IsNone);
        Assert.True(stack.PopOrNone().IsNone);
        Assert.True(stack.PeekOrNone().IsNone);
        Assert.True(priorityQueue.DequeueOrNone().IsNone);
        Assert.True(priorityQueue.PeekOrNone().IsNone);
        Assert.Empty(queue);
        Assert.Empty(stack);
        Assert.Equal(0, priorityQueue.Count);

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        Assert.Throws<InvalidOperationException>(() => queue.Peek());
        Assert.Throws<InvalidOperationException>(() => stack.Pop());
        Assert.Throws<InvalidOperationException>(() => stack.Peek());
        Assert.Throws<InvalidOperationException>(() => priorityQueue.Dequeue());
        Assert.Throws<InvalidOperationException>(() => priorityQueue.Peek());
    }

    [Fact]
    public void IndexOfOrNoneReturnsTheFirstMatchingIndexAndNoneForMissingItems()
    {
        var list = new List<int> { 7, 8, 7 };

        Assert.True(list.IndexOfOrNone(7).TryGetValue(out var firstSeven));
        Assert.Equal(0, firstSeven);
        Assert.True(list.IndexOfOrNone(8).TryGetValue(out var eight));
        Assert.Equal(1, eight);
        Assert.True(list.IndexOfOrNone(9).IsNone);
        Assert.True(new List<int>().IndexOfOrNone(7).IsNone);

        var immutable = ImmutableList.Create("alpha", "beta", "gamma");

        Assert.True(immutable.IndexOfOrNone("beta").TryGetValue(out var beta));
        Assert.Equal(1, beta);
        Assert.True(immutable.IndexOfOrNone("delta").IsNone);
        Assert.True(ImmutableList<string>.Empty.IndexOfOrNone("alpha").IsNone);
    }

    [Fact]
    public void RemoveOrNoneRemovesPresentKeysAndReturnsTheirValues()
    {
        var dictionary = new Dictionary<string, int>
        {
            ["a"] = 1,
            ["b"] = 2,
        };

        var removed = dictionary.RemoveOrNone("a");

        Assert.True(removed.TryGetValue(out var value));
        Assert.Equal(1, value);
        Assert.Single(dictionary);
        Assert.False(dictionary.ContainsKey("a"));
        Assert.True(dictionary.RemoveOrNone("a").IsNone);
        Assert.Single(dictionary);
    }

    [Fact]
    public void RemoveOrNoneReturnsNoneForAbsentKeysWithoutMutating()
    {
        var dictionary = new Dictionary<string, int> { ["a"] = 1 };

        Assert.True(dictionary.RemoveOrNone("missing").IsNone);
        Assert.Single(dictionary);
        Assert.True(dictionary.ContainsKey("a"));
        Assert.True(new Dictionary<string, int>().RemoveOrNone("a").IsNone);
    }
}
