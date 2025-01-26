namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolQueueTests
{
    [TestMethod]
    public void Count()
    {
        var queue = new ArrayPoolQueue<int>();
        Assert.AreEqual(0, queue.Count);

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
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
    public void Capacity()
    {
        var queue = new ArrayPoolQueue<int>(48);
        Assert.AreEqual(64, queue.Capacity);


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Capacity);
    }

    [TestMethod]
    public void Ctor()
    {
        using var empty = new ArrayPoolQueue<int>();
        Assert.AreEqual(0, empty.Count);
        Assert.AreEqual(16, empty.Capacity);

        using var withCapacity = new ArrayPoolQueue<int>(192);
        Assert.AreEqual(0, withCapacity.Count);
        Assert.AreEqual(256, withCapacity.Capacity);

        using var withSource = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        Assert.AreEqual(100, withSource.Count);
        Assert.AreEqual(128, withSource.Capacity);
    }

    [TestMethod]
    public void AsSpan()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(1, 3));
        ArrayPoolQueue<int>.AsSpan(queue, out var head, out var tail);

        CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, head.ToArray().Concat(tail.ToArray()).ToArray());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolQueue<int>.AsSpan(queue, out _, out _));
    }

    [TestMethod]
    public void Clear()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        queue.Clear();
        Assert.AreEqual(0, queue.Count);

        // should not throw any exceptions
        queue.Clear();
        Assert.AreEqual(0, queue.Count);


        queue.Enqueue(1);
        var enumerable = queue.GetEnumerator();
        queue.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerable.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Clear());
    }

    [TestMethod]
    public void Contains()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(queue.Contains(i));
        }

        Assert.IsFalse(queue.Contains(-1));


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Contains(1));
    }

    [TestMethod]
    public void CopyTo()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        queue.CopyTo(buffer, 1);
        for (int i = 1; i <= 100; i++)
        {
            Assert.AreEqual(i - 1, buffer[i]);
        }

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.CopyTo(buffer, -1));
        Assert.ThrowsException<ArgumentException>(() => queue.CopyTo(buffer, 122));


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.CopyTo(buffer, 0));
    }

    [TestMethod]
    public void Dequeue()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, queue.Dequeue());
            Assert.AreEqual(99 - i, queue.Count);
        }

        Assert.ThrowsException<InvalidOperationException>(() => queue.Dequeue());


        queue.Enqueue(1);
        var enumerator = queue.GetEnumerator();
        queue.Dequeue();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Dequeue());
    }

    [TestMethod]
    public void Enqueue()
    {
        var queue = new ArrayPoolQueue<int>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.AreEqual(i + 1, queue.Count);
        }


        var enumerator = queue.GetEnumerator();
        queue.Enqueue(100);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Enqueue(-1));
    }

    [TestMethod]
    public void EnqueueRange()
    {
        var queue = new ArrayPoolQueue<int>();

        for (int i = 0; i < 100; i += 10)
        {
            queue.EnqueueRange(Enumerable.Range(i, 10));
            Assert.AreEqual(i + 10, queue.Count);
            CollectionAssert.AreEqual(Enumerable.Range(0, i + 10).ToArray(), queue);
        }

        for (int i = 100; i < 200; i += 10)
        {
            queue.EnqueueRange(Enumerable.Range(i, 10).ToArray().AsSpan());
            Assert.AreEqual(i + 10, queue.Count);
            CollectionAssert.AreEqual(Enumerable.Range(0, i + 10).ToArray(), queue);
        }


        var enumerator = queue.GetEnumerator();
        queue.EnqueueRange(Enumerable.Range(0, 10).ToArray());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 10)));
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 10).ToArray()));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var queue = new ArrayPoolQueue<int>();
        Assert.AreEqual(64, queue.EnsureCapacity(48));
        Assert.AreEqual(64, queue.EnsureCapacity(1));


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.EnsureCapacity(-1));


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.EnsureCapacity(100));

    }

    [TestMethod]
    public void GetEnumerator()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        int i = 0;
        foreach (var element in queue)
        {
            Assert.AreEqual(i, element);
            i++;
        }

        var enumerator = queue.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        for (i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(i, enumerator.Current);
        }
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Reset());
    }

    [TestMethod]
    public void Peek()
    {
        var queue = new ArrayPoolQueue<int>();

        Assert.ThrowsException<InvalidOperationException>(() => queue.Peek());

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.AreEqual(0, queue.Peek());
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, queue.Peek());
            queue.Dequeue();
        }

        Assert.ThrowsException<InvalidOperationException>(() => queue.Peek());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.Peek());
    }

    [TestMethod]
    public void SetCount()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(1, 6));

        ArrayPoolQueue<int>.SetCount(queue, 12);
        Assert.AreEqual(12, queue.Count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ArrayPoolQueue<int>.SetCount(queue, -1));
        Assert.ThrowsException<ArgumentException>(() => ArrayPoolQueue<int>.SetCount(queue, 99));


        var enumerator = queue.GetEnumerator();
        ArrayPoolQueue<int>.SetCount(queue, 13);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolQueue<int>.SetCount(queue, 1));
    }

    [TestMethod]
    public void ToArray()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        var result = queue.ToArray();
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, result[i]);
        }

        queue.Clear();
        Assert.AreEqual(0, queue.ToArray().Length);


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.ToArray());
    }

    [TestMethod]
    public void TrimExcess()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        queue.TrimExcess();
        Assert.AreEqual(128, queue.Capacity);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.TrimExcess(12));

        queue.Clear();
        queue.TrimExcess();
        Assert.AreEqual(16, queue.Capacity);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => queue.TrimExcess(-1));


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TrimExcess());
    }

    [TestMethod]
    public void TryDequeue()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(queue.TryDequeue(out var value));
            Assert.AreEqual(i, value);
        }

        Assert.IsFalse(queue.TryDequeue(out _));


        queue.Enqueue(1);
        var enumerator = queue.GetEnumerator();
        queue.TryDequeue(out _);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TryDequeue(out _));
    }

    [TestMethod]
    public void TryPeek()
    {
        var queue = new ArrayPoolQueue<int>();

        Assert.IsFalse(queue.TryPeek(out _));

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.IsTrue(queue.TryPeek(out var value));
            Assert.AreEqual(0, value);
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(queue.TryPeek(out var value));
            Assert.AreEqual(i, value);
            queue.Dequeue();
        }

        Assert.ThrowsException<InvalidOperationException>(() => queue.Peek());


        queue.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => queue.TryPeek(out _));
    }
}
