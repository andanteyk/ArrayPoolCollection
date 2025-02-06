using System.Collections;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolPriorityQueueTests
{
    [Fact]
    public void Comparer()
    {
        var emptyValue = new ArrayPoolPriorityQueue<int, int>();
        Assert.Equal(Comparer<int>.Default, emptyValue.Comparer);

        var emptyClass = new ArrayPoolPriorityQueue<string, string>();
        Assert.Equal(Comparer<string>.Default, emptyClass.Comparer);

        var specified = new ArrayPoolPriorityQueue<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, specified.Comparer);


        emptyValue.Dispose();
        emptyClass.Dispose();
        specified.Dispose();
        Assert.Throws<ObjectDisposedException>(() => emptyValue.Comparer);
        Assert.Throws<ObjectDisposedException>(() => emptyClass.Comparer);
        Assert.Throws<ObjectDisposedException>(() => specified.Comparer);
    }

    [Fact]
    public void Capacity()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.Equal(128, queue.Capacity);


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Capacity);
    }

    [Fact]
    public void Count()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
            Assert.Equal(i + 1, queue.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            queue.Dequeue();
            Assert.Equal(99 - i, queue.Count);
        }


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Count);
    }

    [Fact]
    public void UnorderedItems()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        var unorderedItems = queue.UnorderedItems;

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
        }

        Assert.Equal(100, unorderedItems.Count);
        var elementSet = new HashSet<int>();
        foreach (var (element, priority) in unorderedItems)
        {
            Assert.True(element == priority);
            elementSet.Add(element);
        }

        Assert.Equivalent(elementSet.ToArray(), Enumerable.Range(0, 100).ToArray());


        var buffer = new (int element, int priority)[128];
        ((ICollection)unorderedItems).CopyTo(buffer, 1);
        Assert.Equivalent(elementSet.ToArray(), buffer.Skip(1).Take(100).Select(pair => pair.element).ToArray());
        Assert.Equivalent(elementSet.ToArray(), buffer.Skip(1).Take(100).Select(pair => pair.priority).ToArray());


        var enumerator = unorderedItems.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            _ = enumerator.Current;
        }
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.True(enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.UnorderedItems);
        Assert.Throws<ObjectDisposedException>(() => unorderedItems.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => unorderedItems.Count);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Ctor()
    {
        using var empty = new ArrayPoolPriorityQueue<int, int>();
        Assert.Equal(16, empty.Capacity);

        using var withCapacity = new ArrayPoolPriorityQueue<int, int>(256);
        Assert.Equal(256, withCapacity.Capacity);

        using var withComparer = new ArrayPoolPriorityQueue<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, withComparer.Comparer);

        using var withCapacityAndComparer = new ArrayPoolPriorityQueue<string, string>(256, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(256, withCapacityAndComparer.Capacity);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, withCapacityAndComparer.Comparer);

        using var withSource = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.Equal(100, withSource.Count);

        using var withSourceAndComparer = new ArrayPoolPriorityQueue<string, int>(Enumerable.Range(0, 100).Select(i => (i.ToString(), i)), Comparer<int>.Default);
        Assert.Equal(100, withSourceAndComparer.Count);
        Assert.Equal(Comparer<int>.Default, withSourceAndComparer.Comparer);
    }

    [Fact]
    public void AsSpan()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(1, 15).Select(i => (i, i)));
        var span = ArrayPoolPriorityQueue<int, int>.AsSpan(queue);

        Assert.Equivalent(Enumerable.Range(1, 15).Select(i => (i, i)).ToArray(), span.ToArray());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolPriorityQueue<int, int>.AsSpan(queue));
    }

    [Fact]
    public void Clear()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.Equal(100, queue.Count);

        queue.Clear();
        Assert.Equal(0, queue.Count);


        queue.Enqueue(0, 0);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Clear());
    }

    [Fact]
    public void Dequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));

        for (int i = 99; i >= 0; i--)
        {
            Assert.Equal(99 - i, queue.Dequeue());
        }

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());


        queue.Enqueue(1, 1);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Dequeue();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Dequeue());
    }

    [Fact]
    public void DequeueEnqueue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.Throws<InvalidOperationException>(() => queue.DequeueEnqueue(1, 1));

        queue.Enqueue(1, 1);
        Assert.Equal(1, queue.DequeueEnqueue(2, 2));
        Assert.Equal(2, queue.DequeueEnqueue(3, 3));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.DequeueEnqueue(4, 4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.DequeueEnqueue(1, 1));
    }

    [Fact]
    public void Enqueue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        for (int i = 99; i >= 0; i--)
        {
            queue.Enqueue(i, i);
            Assert.Equal(100 - i, queue.Count);
            Assert.Equal(i, queue.Peek());
        }


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Enqueue(4, 4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Enqueue(1, 1));
    }

    [Fact]
    public void EnqueueDequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        Assert.Equal(0, queue.EnqueueDequeue(0, 0));

        queue.Enqueue(1, 1);
        Assert.Equal(1, queue.EnqueueDequeue(0, 0));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnqueueDequeue(4, 4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.EnqueueDequeue(1, 1));
    }

    private static IEnumerable<T> HideLength<T>(IEnumerable<T> source)
    {
        foreach (var element in source)
        {
            yield return element;
        }
    }

    [Fact]
    public void EnqueueRange()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        queue.EnqueueRange(HideLength(Enumerable.Range(0, 100).Select(i => (i, i))));

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, queue.Dequeue());
        }


        queue.EnqueueRange(HideLength(Enumerable.Range(0, 100)), 0);
        Assert.Equivalent(Enumerable.Range(0, 100).Select(i => (i, 0)), queue.UnorderedItems);


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnqueueRange(Enumerable.Range(0, 100), 0);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 100), 0));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.Equal(256, queue.EnsureCapacity(192));
        Assert.Equal(256, queue.Capacity);
        Assert.Equal(256, queue.EnsureCapacity(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.EnsureCapacity(-1));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnsureCapacity(512);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.EnsureCapacity(1));
    }

    [Fact]
    public void Peek()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        Assert.Throws<InvalidOperationException>(() => queue.Peek());

        for (int i = 99; i >= 0; i--)
        {
            queue.Enqueue(i, i);
            Assert.Equal(i, queue.Peek());
        }


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Peek());
    }

    [Fact]
    public void SetCount()
    {
        var stack = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(1, 6).Select(i => (i, i)));

        ArrayPoolPriorityQueue<int, int>.SetCount(stack, 12);
        Assert.Equal(12, stack.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, -1));
        Assert.Throws<ArgumentException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, 99));


        var enumerator = stack.UnorderedItems.GetEnumerator();
        ArrayPoolPriorityQueue<int, int>.SetCount(stack, 13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, 1));
    }

    [Fact]
    public void TrimExcess()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(256);
        queue.TrimExcess();
        Assert.Equal(16, queue.Capacity);


        queue.EnsureCapacity(100);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.TrimExcess();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TrimExcess());
    }

    [Fact]
    public void TryDequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.False(queue.TryDequeue(out _, out _));

        queue.Enqueue(1, 2);
        Assert.True(queue.TryDequeue(out var element, out var priority));
        Assert.Equal(1, element);
        Assert.Equal(2, priority);
        Assert.False(queue.TryDequeue(out _, out _));


        queue.Enqueue(1, 2);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        Assert.True(queue.TryDequeue(out _, out _));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TryDequeue(out _, out _));
    }

    [Fact]
    public void TryPeek()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.False(queue.TryPeek(out _, out _));

        queue.Enqueue(1, 2);
        Assert.True(queue.TryPeek(out var element, out var priority));
        Assert.Equal(1, element);
        Assert.Equal(2, priority);
        Assert.True(queue.TryPeek(out _, out _));


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TryPeek(out _, out _));
    }

    [Fact]
    public void Dispose()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));

        queue.Dispose();
        // should not throw any exceptions
        queue.Dispose();
    }

#if NET6_0_OR_GREATER
    [Fact]
    public void Monkey()
    {
        var rng = new Random(0);

        var expect = new PriorityQueue<long, long>();
        using var actual = new ArrayPoolPriorityQueue<long, long>();

        for (int i = 0; i < 1024 * 1024; i++)
        {
            if (rng.NextDouble() < 0.25)
            {
                expect.TryDequeue(out _, out _);
                actual.TryDequeue(out _, out _);
            }
            else
            {
                long element = rng.NextInt64();
                long priority = rng.NextInt64();
                expect.Enqueue(element, priority);
                actual.Enqueue(element, priority);
            }

            Assert.Equal(expect.Peek(), actual.Peek());
        }

        while (expect.Count > 0)
        {
            Assert.Equal(expect.Dequeue(), actual.Dequeue());
        }
        Assert.Equal(0, actual.Count);
    }
#endif
}
