namespace ArrayPoolCollection.Tests;

public class ArrayPoolQueueTests
{
    [Fact]
    public void Count()
    {
        var queue = new ArrayPoolQueue<int>();
        Assert.Empty(queue);

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
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
    public void Capacity()
    {
        var queue = new ArrayPoolQueue<int>(48);
        Assert.Equal(64, queue.Capacity);


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Capacity);
    }

    [Fact]
    public void Ctor()
    {
        using var empty = new ArrayPoolQueue<int>();
        Assert.Empty(empty);
        Assert.Equal(16, empty.Capacity);

        using var withCapacity = new ArrayPoolQueue<int>(192);
        Assert.Empty(withCapacity);
        Assert.Equal(256, withCapacity.Capacity);

        using var withSource = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        Assert.Equal(100, withSource.Count);
        Assert.Equal(128, withSource.Capacity);
    }

    [Fact]
    public void AsSpan()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(1, 3));
        ArrayPoolQueue<int>.AsSpan(queue, out var head, out var tail);

        Assert.Equal(new int[] { 1, 2, 3 }, head.ToArray().Concat(tail.ToArray()).ToArray());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolQueue<int>.AsSpan(queue, out _, out _));
    }

    [Fact]
    public void Clear()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        queue.Clear();
        Assert.Empty(queue);

        // should not throw any exceptions
        queue.Clear();
        Assert.Empty(queue);


        queue.Enqueue(1);
        var enumerable = queue.GetEnumerator();
        queue.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerable.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Clear());
    }

    [Fact]
    public void Contains()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.True(queue.Contains(i));
        }

        Assert.False(queue.Contains(-1));


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Contains(1));
    }

    [Fact]
    public void CopyTo()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        queue.CopyTo(buffer, 1);
        for (int i = 1; i <= 100; i++)
        {
            Assert.Equal(i - 1, buffer[i]);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => queue.CopyTo(buffer, -1));
        Assert.Throws<ArgumentException>(() => queue.CopyTo(buffer, 122));


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.CopyTo(buffer, 0));
    }

    [Fact]
    public void Dequeue()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, queue.Dequeue());
            Assert.Equal(99 - i, queue.Count);
        }

        Assert.Throws<InvalidOperationException>(() => queue.Dequeue());


        queue.Enqueue(1);
        var enumerator = queue.GetEnumerator();
        queue.Dequeue();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Dequeue());
    }

    [Fact]
    public void Enqueue()
    {
        var queue = new ArrayPoolQueue<int>();

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.Equal(i + 1, queue.Count);
        }


        var enumerator = queue.GetEnumerator();
        queue.Enqueue(100);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Enqueue(-1));
    }

    [Fact]
    public void EnqueueRange()
    {
        var queue = new ArrayPoolQueue<int>();

        for (int i = 0; i < 100; i += 10)
        {
            queue.EnqueueRange(Enumerable.Range(i, 10));
            Assert.Equal(i + 10, queue.Count);
            Assert.Equal(Enumerable.Range(0, i + 10).ToArray(), queue);
        }

        for (int i = 100; i < 200; i += 10)
        {
            queue.EnqueueRange(Enumerable.Range(i, 10).ToArray().AsSpan());
            Assert.Equal(i + 10, queue.Count);
            Assert.Equal(Enumerable.Range(0, i + 10).ToArray(), queue);
        }


        var enumerator = queue.GetEnumerator();
        queue.EnqueueRange(Enumerable.Range(0, 10).ToArray());
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 10)));
        Assert.Throws<ObjectDisposedException>(() => queue.EnqueueRange(Enumerable.Range(0, 10).ToArray()));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var queue = new ArrayPoolQueue<int>();
        Assert.Equal(64, queue.EnsureCapacity(48));
        Assert.Equal(64, queue.EnsureCapacity(1));


        Assert.Throws<ArgumentOutOfRangeException>(() => queue.EnsureCapacity(-1));


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.EnsureCapacity(100));

    }

    [Fact]
    public void GetEnumerator()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        int i = 0;
        foreach (var element in queue)
        {
            Assert.Equal(i, element);
            i++;
        }

        var enumerator = queue.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
    }

    [Fact]
    public void Peek()
    {
        var queue = new ArrayPoolQueue<int>();

        Assert.Throws<InvalidOperationException>(() => queue.Peek());

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.Equal(0, queue.Peek());
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, queue.Peek());
            queue.Dequeue();
        }

        Assert.Throws<InvalidOperationException>(() => queue.Peek());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.Peek());
    }

    [Fact]
    public void SetCount()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(1, 6));

        ArrayPoolQueue<int>.SetCount(queue, 12);
        Assert.Equal(12, queue.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolQueue<int>.SetCount(queue, -1));
        Assert.Throws<ArgumentException>(() => ArrayPoolQueue<int>.SetCount(queue, 99));


        var enumerator = queue.GetEnumerator();
        ArrayPoolQueue<int>.SetCount(queue, 13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolQueue<int>.SetCount(queue, 1));
    }

    [Fact]
    public void ToArray()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        var result = queue.ToArray();
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, result[i]);
        }

        queue.Clear();
        Assert.Empty(queue.ToArray());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.ToArray());
    }

    [Fact]
    public void TrimExcess()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        queue.TrimExcess();
        Assert.Equal(128, queue.Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() => queue.TrimExcess(12));

        queue.Clear();
        queue.TrimExcess();
        Assert.Equal(16, queue.Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() => queue.TrimExcess(-1));


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TrimExcess());
    }

    [Fact]
    public void TryDequeue()
    {
        var queue = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.True(queue.TryDequeue(out var value));
            Assert.Equal(i, value);
        }

        Assert.False(queue.TryDequeue(out _));


        queue.Enqueue(1);
        var enumerator = queue.GetEnumerator();
        queue.TryDequeue(out _);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TryDequeue(out _));
    }

    [Fact]
    public void TryPeek()
    {
        var queue = new ArrayPoolQueue<int>();

        Assert.False(queue.TryPeek(out _));

        for (int i = 0; i < 100; i++)
        {
            queue.Enqueue(i);
            Assert.True(queue.TryPeek(out var value));
            Assert.Equal(0, value);
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.True(queue.TryPeek(out var value));
            Assert.Equal(i, value);
            queue.Dequeue();
        }

        Assert.Throws<InvalidOperationException>(() => queue.Peek());


        queue.Dispose();
        Assert.Throws<ObjectDisposedException>(() => queue.TryPeek(out _));
    }
}
