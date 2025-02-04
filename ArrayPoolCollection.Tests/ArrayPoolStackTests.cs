namespace ArrayPoolCollection.Tests;

public class ArrayPoolStackTests
{
    [Fact]
    public void Count()
    {
        var stack = new ArrayPoolStack<int>();
        Assert.Empty(stack);

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.Equal(i + 1, stack.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            stack.Pop();
            Assert.Equal(99 - i, stack.Count);
        }


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Count);
    }

    [Fact]
    public void Capacity()
    {
        var stack = new ArrayPoolStack<int>();
        Assert.Equal(16, stack.Capacity);

        using var stackWithCapacity = new ArrayPoolStack<int>(48);
        Assert.Equal(64, stackWithCapacity.Capacity);


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Capacity);
    }

    [Fact]
    public void Ctor()
    {
        using var stack = new ArrayPoolStack<int>();
        Assert.Empty(stack);

        using var stackWithCapacity = new ArrayPoolStack<int>(64);
        Assert.Empty(stackWithCapacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolStack<int>(-1));

        using var stackWithSource = new ArrayPoolStack<int>(Enumerable.Range(0, 100));
        for (int i = 0; i < 100; i++)
        {
            Assert.True(stackWithSource.Contains(i));
        }
        Assert.Equal(100, stackWithSource.Count);
    }

    [Fact]
    public void Dispose()
    {
        var stack = new ArrayPoolStack<int>();

        // should not throw any exceptions
        stack.Dispose();
        stack.Dispose();
    }

    [Fact]
    public void AsSpan()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(1, 3));
        var span = ArrayPoolStack<int>.AsSpan(stack);

        Assert.Equal(new int[] { 1, 2, 3 }, span.ToArray());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolStack<int>.AsSpan(stack));
    }

    [Fact]
    public void Clear()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        stack.Clear();
        Assert.Empty(stack);

        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        stack.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Clear());
    }

    [Fact]
    public void Contains()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.True(stack.Contains(0));
        Assert.True(stack.Contains(99));
        Assert.False(stack.Contains(100));


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Contains(0));
    }

    [Fact]
    public void CopyTo()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.CopyTo(buffer, -1));
        Assert.Throws<ArgumentException>(() => stack.CopyTo(buffer, 100));

        stack.CopyTo(buffer, 1);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i, buffer[i + 1]);
        }

        stack.CopyTo(buffer.AsSpan(1..));
        Assert.Throws<ArgumentException>(() => stack.CopyTo(buffer.AsSpan(100..)));


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.CopyTo(buffer, 0));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.Equal(256, stack.EnsureCapacity(192));
        Assert.Equal(256, stack.EnsureCapacity(100));
        Assert.Equal(256, stack.EnsureCapacity(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => stack.EnsureCapacity(-1));


        var enumerator = stack.GetEnumerator();
        stack.EnsureCapacity(512);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.EnsureCapacity(100));
    }

    [Fact]
    public void GetEnumerator()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        int i = 0;
        foreach (var element in stack)
        {
            Assert.Equal(i++, element);
        }


        var enumerator = stack.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
    }

    [Fact]
    public void Peek()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.Equal(99, stack.Peek());
        Assert.Equal(99, stack.Peek());

        stack.Push(100);
        Assert.Equal(100, stack.Peek());

        stack.Pop();
        Assert.Equal(99, stack.Peek());


        stack.Clear();
        Assert.Throws<InvalidOperationException>(() => stack.Peek());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Peek());
    }

    [Fact]
    public void Pop()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        for (int i = 99; i >= 0; i--)
        {
            Assert.Equal(i, stack.Pop());
            Assert.Equal(i, stack.Count);
        }

        Assert.Throws<InvalidOperationException>(() => stack.Pop());


        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        stack.Pop();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Pop());
    }

    [Fact]
    public void Push()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.Equal(i, stack.Peek());
            Assert.Equal(i + 1, stack.Count);
        }


        var enumerator = stack.GetEnumerator();
        stack.Push(100);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.Push(1));
    }

    [Fact]
    public void PushRange()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i += 10)
        {
            stack.PushRange(Enumerable.Range(i, 10));
            Assert.Equal(i + 9, stack.Peek());
            Assert.Equal(i + 10, stack.Count);
        }

        for (int i = 0; i < 100; i += 10)
        {
            stack.PushRange(Enumerable.Range(i, 10).ToArray());
            Assert.Equal(i + 9, stack.Peek());
            Assert.Equal(i + 110, stack.Count);
        }


        var enumerator = stack.GetEnumerator();
        stack.PushRange(Enumerable.Range(0, 10));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.PushRange(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void SetCount()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(1, 6));

        ArrayPoolStack<int>.SetCount(stack, 12);
        Assert.Equal(12, stack.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolStack<int>.SetCount(stack, -1));
        Assert.Throws<ArgumentException>(() => ArrayPoolStack<int>.SetCount(stack, 99));


        var enumerator = stack.GetEnumerator();
        ArrayPoolStack<int>.SetCount(stack, 13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolStack<int>.SetCount(stack, 1));
    }

    [Fact]
    public void ToArray()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(Enumerable.Range(0, i).ToArray(), stack.ToArray());
            stack.Push(i);
        }


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.ToArray());
    }

    [Fact]
    public void TrimExcess()
    {
        var stack = new ArrayPoolStack<int>(256);

        stack.TrimExcess();
        Assert.Equal(16, stack.Capacity);

        stack.EnsureCapacity(128);
        stack.TrimExcess(48);
        Assert.Equal(64, stack.Capacity);


        stack.Push(1);
        Assert.Throws<ArgumentOutOfRangeException>(() => stack.TrimExcess(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stack.TrimExcess(0));


        var enumerator = stack.GetEnumerator();
        stack.TrimExcess(48);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.TrimExcess());
    }

    [Fact]
    public void TryPeek()
    {
        var stack = new ArrayPoolStack<int>();

        Assert.False(stack.TryPeek(out _));

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.True(stack.TryPeek(out var value));
            Assert.Equal(i, value);
        }


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.TryPeek(out _));
    }

    [Fact]
    public void TryPop()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        for (int i = 99; i >= 0; i--)
        {
            Assert.True(stack.TryPop(out var value));
            Assert.Equal(i, value);
            Assert.Equal(i, stack.Count);
        }

        Assert.False(stack.TryPop(out _));


        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        Assert.True(stack.TryPop(out _));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stack.TryPop(out _));
    }
}
