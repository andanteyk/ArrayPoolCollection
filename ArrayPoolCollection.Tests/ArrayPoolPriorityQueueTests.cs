using System.Collections;
using System.Reflection;
using System.Reflection.Metadata;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolPriorityQueueTests
{
    [TestMethod]
    public void Comparer()
    {
        var emptyValue = new ArrayPoolPriorityQueue<int, int>();
        Assert.AreEqual(Comparer<int>.Default, emptyValue.Comparer);

        var emptyClass = new ArrayPoolPriorityQueue<string, string>();
        Assert.AreEqual(Comparer<string>.Default, emptyClass.Comparer);

        var specified = new ArrayPoolPriorityQueue<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, specified.Comparer);


        emptyValue.Dispose();
        emptyClass.Dispose();
        specified.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => emptyValue.Comparer);
        Assert.ThrowsException<ObjectDisposedException>(() => emptyClass.Comparer);
        Assert.ThrowsException<ObjectDisposedException>(() => specified.Comparer);
    }

    [TestMethod]
    public void Capacity()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.AreEqual(128, queue.Capacity);


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Capacity);
    }

    [TestMethod]
    public void Count()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
            Assert.AreEqual(i + 1, queue.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            queue.Dequeue();
            Assert.AreEqual(99 - i, queue.Count);
        }


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Count);
    }

    [TestMethod]
    public void UnorderedItems()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        var unorderedItems = queue.UnorderedItems;

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
        }

        Assert.AreEqual(100, unorderedItems.Count);
        var elementSet = new HashSet<int>();
        foreach (var (element, priority) in unorderedItems)
        {
            Assert.IsTrue(element == priority);
            elementSet.Add(element);
        }

        CollectionAssert.AreEquivalent(elementSet.ToArray(), Enumerable.Range(0, 100).ToArray());


        var buffer = new (int element, int priority)[128];
        ((ICollection)unorderedItems).CopyTo(buffer, 1);
        CollectionAssert.AreEquivalent(elementSet.ToArray(), buffer.Skip(1).Take(100).Select(pair => pair.element).ToArray());
        CollectionAssert.AreEquivalent(elementSet.ToArray(), buffer.Skip(1).Take(100).Select(pair => pair.priority).ToArray());


        var enumerator = unorderedItems.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            _ = enumerator.Current;
        }
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.IsTrue(enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.UnorderedItems);
        Assert.ThrowsException<ObjectDisposedException>(() => unorderedItems.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => unorderedItems.Count);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Ctor()
    {
        using var empty = new ArrayPoolPriorityQueue<int, int>();
        Assert.AreEqual(16, empty.Capacity);

        using var withCapacity = new ArrayPoolPriorityQueue<int, int>(256);
        Assert.AreEqual(256, withCapacity.Capacity);

        using var withComparer = new ArrayPoolPriorityQueue<string, string>(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, withComparer.Comparer);

        using var withCapacityAndComparer = new ArrayPoolPriorityQueue<string, string>(256, StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(256, withCapacityAndComparer.Capacity);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, withCapacityAndComparer.Comparer);

        using var withSource = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.AreEqual(100, withSource.Count);

        using var withSourceAndComparer = new ArrayPoolPriorityQueue<string, int>(Enumerable.Range(0, 100).Select(i => (i.ToString(), i)), Comparer<int>.Default);
        Assert.AreEqual(100, withSourceAndComparer.Count);
        Assert.AreEqual(Comparer<int>.Default, withSourceAndComparer.Comparer);
    }

    [TestMethod]
    public void AsSpan()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(1, 15).Select(i => (i, i)));
        var span = ArrayPoolPriorityQueue<int, int>.AsSpan(queue);

        CollectionAssert.AreEquivalent(Enumerable.Range(1, 15).Select(i => (i, i)).ToArray(), span.ToArray());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolPriorityQueue<int, int>.AsSpan(queue));
    }

    [TestMethod]
    public void Clear()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));
        Assert.AreEqual(100, queue.Count);

        queue.Clear();
        Assert.AreEqual(0, queue.Count);


        queue.Enqueue(0, 0);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Clear());
    }

    [TestMethod]
    public void Dequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(99 - i, queue.Dequeue());
        }

        Assert.ThrowsException<InvalidOperationException>(() => queue.Dequeue());


        queue.Enqueue(1, 1);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Dequeue();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Dequeue());
    }

    [TestMethod]
    public void DequeueEnqueue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.ThrowsException<InvalidOperationException>(() => queue.DequeueEnqueue(1, 1));

        queue.Enqueue(1, 1);
        Assert.AreEqual(1, queue.DequeueEnqueue(2, 2));
        Assert.AreEqual(2, queue.DequeueEnqueue(3, 3));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.DequeueEnqueue(4, 4);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.DequeueEnqueue(1, 1));
    }

    [TestMethod]
    public void Enqueue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
            Assert.AreEqual(i + 1, queue.Count);
            Assert.AreEqual(i, queue.Peek());
        }


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.Enqueue(4, 4);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Enqueue(1, 1));
    }

    [TestMethod]
    public void EnqueueDequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        Assert.AreEqual(0, queue.EnqueueDequeue(0, 0));

        queue.Enqueue(1, 1);
        Assert.AreEqual(1, queue.EnqueueDequeue(0, 0));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnqueueDequeue(4, 4);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnqueueDequeue(1, 1));
    }

    [TestMethod]
    public void EnqueueRange()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        queue.EnqueueRange(Enumerable.Range(0, 100).Select(i => (i, i)));

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(99 - i, queue.Dequeue());
        }


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnqueueRange(Enumerable.Range(0, 100), 0);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 100), 0));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.AreEqual(256, queue.EnsureCapacity(192));
        Assert.AreEqual(256, queue.Capacity);
        Assert.AreEqual(256, queue.EnsureCapacity(1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.EnsureCapacity(-1));


        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.EnsureCapacity(512);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnsureCapacity(1));
    }

    [TestMethod]
    public void Peek()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();

        Assert.ThrowsException<InvalidOperationException>(() => queue.Peek());

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i, i);
            Assert.AreEqual(i, queue.Peek());
        }


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Peek());
    }

    [TestMethod]
    public void SetCount()
    {
        var stack = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(1, 6).Select(i => (i, i)));

        ArrayPoolPriorityQueue<int, int>.SetCount(stack, 12);
        Assert.AreEqual(12, stack.Count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, -1));
        Assert.ThrowsException<ArgumentException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, 99));


        var enumerator = stack.UnorderedItems.GetEnumerator();
        ArrayPoolPriorityQueue<int, int>.SetCount(stack, 13);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolPriorityQueue<int, int>.SetCount(stack, 1));
    }

    [TestMethod]
    public void TrimExcess()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(256);
        queue.TrimExcess();
        Assert.AreEqual(16, queue.Capacity);


        queue.EnsureCapacity(100);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        queue.TrimExcess();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TrimExcess());
    }

    [TestMethod]
    public void TryDequeue()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.IsFalse(queue.TryDequeue(out _, out _));

        queue.Enqueue(1, 2);
        Assert.IsTrue(queue.TryDequeue(out var element, out var priority));
        Assert.AreEqual(1, element);
        Assert.AreEqual(2, priority);
        Assert.IsFalse(queue.TryDequeue(out _, out _));


        queue.Enqueue(1, 2);
        var enumerator = queue.UnorderedItems.GetEnumerator();
        Assert.IsTrue(queue.TryDequeue(out _, out _));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TryDequeue(out _, out _));
    }

    [TestMethod]
    public void TryPeek()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>();
        Assert.IsFalse(queue.TryPeek(out _, out _));

        queue.Enqueue(1, 2);
        Assert.IsTrue(queue.TryPeek(out var element, out var priority));
        Assert.AreEqual(1, element);
        Assert.AreEqual(2, priority);
        Assert.IsTrue(queue.TryPeek(out _, out _));


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TryPeek(out _, out _));
    }

    [TestMethod]
    public void Dispose()
    {
        var queue = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));

        queue.Dispose();
        // should not throw any exceptions
        queue.Dispose();
    }
}
