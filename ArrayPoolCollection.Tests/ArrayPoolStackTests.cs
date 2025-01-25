using System.Collections.ObjectModel;
using System.Reflection.Metadata;
using System.Runtime;
using System.Security.Cryptography;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolStackTests
{
    [TestMethod]
    public void Count()
    {
        var stack = new ArrayPoolStack<int>();
        Assert.AreEqual(0, stack.Count);

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.AreEqual(i + 1, stack.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            stack.Pop();
            Assert.AreEqual(99 - i, stack.Count);
        }


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Count);
    }

    [TestMethod]
    public void Capacity()
    {
        var stack = new ArrayPoolStack<int>();
        Assert.AreEqual(16, stack.Capacity);

        using var stackWithCapacity = new ArrayPoolStack<int>(48);
        Assert.AreEqual(64, stackWithCapacity.Capacity);


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Capacity);
    }

    [TestMethod]
    public void Ctor()
    {
        using var stack = new ArrayPoolStack<int>();
        Assert.AreEqual(0, stack.Count);

        using var stackWithCapacity = new ArrayPoolStack<int>(64);
        Assert.AreEqual(0, stackWithCapacity.Count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolStack<int>(-1));

        using var stackWithSource = new ArrayPoolStack<int>(Enumerable.Range(0, 100));
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(stackWithSource.Contains(i));
        }
        Assert.AreEqual(100, stackWithSource.Count);
    }

    [TestMethod]
    public void Dispose()
    {
        var stack = new ArrayPoolStack<int>();

        // should not throw any exceptions
        stack.Dispose();
        stack.Dispose();
    }

    [TestMethod]
    public void Clear()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        stack.Clear();
        Assert.AreEqual(0, stack.Count);

        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        stack.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Clear());
    }

    [TestMethod]
    public void Contains()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.IsTrue(stack.Contains(0));
        Assert.IsTrue(stack.Contains(99));
        Assert.IsFalse(stack.Contains(100));


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Contains(0));
    }

    [TestMethod]
    public void CopyTo()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.CopyTo(buffer, -1));
        Assert.ThrowsException<ArgumentException>(() => stack.CopyTo(buffer, 100));

        stack.CopyTo(buffer, 1);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i, buffer[i + 1]);
        }

        stack.CopyTo(buffer.AsSpan(1..));
        Assert.ThrowsException<ArgumentException>(() => stack.CopyTo(buffer.AsSpan(100..)));


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.CopyTo(buffer, 0));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.AreEqual(256, stack.EnsureCapacity(192));
        Assert.AreEqual(256, stack.EnsureCapacity(100));
        Assert.AreEqual(256, stack.EnsureCapacity(1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.EnsureCapacity(-1));


        var enumerator = stack.GetEnumerator();
        stack.EnsureCapacity(512);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.EnsureCapacity(100));
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        int i = 0;
        foreach (var element in stack)
        {
            Assert.AreEqual(i++, element);
        }


        var enumerator = stack.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        for (i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(i, enumerator.Current);
        }
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Reset());
    }

    [TestMethod]
    public void Peek()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        Assert.AreEqual(99, stack.Peek());
        Assert.AreEqual(99, stack.Peek());

        stack.Push(100);
        Assert.AreEqual(100, stack.Peek());

        stack.Pop();
        Assert.AreEqual(99, stack.Peek());


        stack.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => stack.Peek());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Peek());
    }

    [TestMethod]
    public void Pop()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        for (int i = 99; i >= 0; i--)
        {
            Assert.AreEqual(i, stack.Pop());
            Assert.AreEqual(i, stack.Count);
        }

        Assert.ThrowsException<InvalidOperationException>(() => stack.Pop());


        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        stack.Pop();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Pop());
    }

    [TestMethod]
    public void Push()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.AreEqual(i, stack.Peek());
            Assert.AreEqual(i + 1, stack.Count);
        }


        var enumerator = stack.GetEnumerator();
        stack.Push(100);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.Push(1));
    }

    [TestMethod]
    public void PushRange()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i += 10)
        {
            stack.PushRange(Enumerable.Range(i, 10));
            Assert.AreEqual(i + 9, stack.Peek());
            Assert.AreEqual(i + 10, stack.Count);
        }

        for (int i = 0; i < 100; i += 10)
        {
            stack.PushRange(Enumerable.Range(i, 10).ToArray());
            Assert.AreEqual(i + 9, stack.Peek());
            Assert.AreEqual(i + 110, stack.Count);
        }


        var enumerator = stack.GetEnumerator();
        stack.PushRange(Enumerable.Range(0, 10));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.PushRange(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void ToArray()
    {
        var stack = new ArrayPoolStack<int>();

        for (int i = 0; i < 100; i++)
        {
            CollectionAssert.AreEqual(Enumerable.Range(0, i).ToArray(), stack.ToArray());
            stack.Push(i);
        }


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.ToArray());
    }

    [TestMethod]
    public void TrimExcess()
    {
        var stack = new ArrayPoolStack<int>(256);

        stack.TrimExcess();
        Assert.AreEqual(16, stack.Capacity);

        stack.EnsureCapacity(128);
        stack.TrimExcess(48);
        Assert.AreEqual(64, stack.Capacity);


        stack.Push(1);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.TrimExcess(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => stack.TrimExcess(0));


        var enumerator = stack.GetEnumerator();
        stack.TrimExcess(48);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.TrimExcess());
    }

    [TestMethod]
    public void TryPeek()
    {
        var stack = new ArrayPoolStack<int>();

        Assert.IsFalse(stack.TryPeek(out _));

        for (int i = 0; i < 100; i++)
        {
            stack.Push(i);
            Assert.IsTrue(stack.TryPeek(out var value));
            Assert.AreEqual(i, value);
        }


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.TryPeek(out _));
    }

    [TestMethod]
    public void TryPop()
    {
        var stack = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        for (int i = 99; i >= 0; i--)
        {
            Assert.IsTrue(stack.TryPop(out var value));
            Assert.AreEqual(i, value);
            Assert.AreEqual(i, stack.Count);
        }

        Assert.IsFalse(stack.TryPop(out _));


        stack.Push(1);
        var enumerator = stack.GetEnumerator();
        Assert.IsTrue(stack.TryPop(out _));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        stack.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => stack.TryPop(out _));
    }
}
