namespace ArrayPoolCollection.Tests;

[TestClass]
public class SpanBitSetTests
{
    [TestMethod]
    public void Items()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 128);

        for (int i = 0; i < 128; i++)
        {
            bits[i] = i % 2 == 1;
        }

        for (int i = 0; i < 128; i++)
        {
            Assert.AreEqual(i % 2 == 1, bits[i]);
        }

        try
        {
            _ = bits[-1];
            Assert.Fail();
        }
        catch (IndexOutOfRangeException) { }

        try
        {
            bits[128] = false;
            Assert.Fail();
        }
        catch (IndexOutOfRangeException) { }


        bits.Dispose();
        try
        {
            bits[1] = false;
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Count()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 128);

        Assert.AreEqual(128, bits.Count);

        bits.Add(false);
        Assert.AreEqual(129, bits.Count);

        bits.RemoveAt(0);
        Assert.AreEqual(128, bits.Count);


        bits.Dispose();
        try
        {
            _ = bits.Count;
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Ctor()
    {
        using var fromSpanLength = new SpanBitSet(stackalloc nuint[16], 150);
        Assert.AreEqual(150, fromSpanLength.Count);

        using var fromCapacityLength = new SpanBitSet(1024, 150);
        Assert.AreEqual(150, fromCapacityLength.Count);

        using var fromSpanOverLength = new SpanBitSet(stackalloc nuint[16], 32 * 16);
        Assert.AreEqual(32 * 16, fromSpanOverLength.Count);
    }

    [TestMethod]
    public void Add()
    {
        var bits = new SpanBitSet(stackalloc nuint[1], 0);

        for (int i = 0; i < 1024; i++)
        {
            bits.Add(i % 2 == 1);
            Assert.AreEqual(i % 2 == 1, bits[i]);
            Assert.AreEqual(i + 1, bits.Count);
        }

        var enumerator = bits.GetEnumerator();
        enumerator.MoveNext();
        bits.Add(false);
        try
        {
            enumerator.MoveNext();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        bits.Dispose();
        try
        {
            bits.Add(false);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Clear()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);
        bits.Clear();
        Assert.AreEqual(0, bits.Count);


        var enumerator = bits.GetEnumerator();
        enumerator.MoveNext();
        bits.Clear();
        try
        {
            enumerator.MoveNext();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        bits.Dispose();
        try
        {
            bits.Clear();
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Contains()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);
        Assert.IsTrue(bits.Contains(false));
        Assert.IsFalse(bits.Contains(true));

        bits[0] = true;

        Assert.IsTrue(bits.Contains(false));
        Assert.IsTrue(bits.Contains(true));


        bits.Dispose();
        try
        {
            bits.Contains(false);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void CopyTo()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);
        bits[0] = true;
        bits[99] = true;

        var bools = new bool[128];

        bits.CopyTo(bools, 0);
        Assert.IsTrue(bools[0]);
        Assert.IsTrue(bools[99]);

        bits.CopyTo(bools.AsSpan(1..));
        Assert.IsTrue(bools[1]);
        Assert.IsFalse(bools[99]);
        Assert.IsTrue(bools[100]);

        try
        {
            bits.CopyTo(bools, -1);
            Assert.Fail();
        }
        catch (ArgumentOutOfRangeException) { }

        try
        {
            bits.CopyTo(bools, 30);
            Assert.Fail();
        }
        catch (ArgumentException) { }


        bits.Dispose();
        try
        {
            bits.CopyTo(bools, 0);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Dispose()
    {
        using var fromSpan = new SpanBitSet(stackalloc nuint[16], 0);

        using var fromArray = new SpanBitSet(1024, 0);
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);

        int i = 0;
        foreach (var b in bits)
        {
            i++;
        }
        Assert.AreEqual(64, i);


        var enumerator = bits.GetEnumerator();
        try
        {
            _ = enumerator.Current;
            Assert.Fail();
        }
        catch (InvalidOperationException) { }

        while (enumerator.MoveNext())
        {
            _ = enumerator.Current;
        }

        try
        {
            _ = enumerator.Current;
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        enumerator.Reset();


        bits.Dispose();
        try
        {
            _ = enumerator.MoveNext();
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
        try
        {
            bits.GetEnumerator();
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void IndexOf()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        Assert.AreEqual(-1, bits.IndexOf(true));
        Assert.AreEqual(0, bits.IndexOf(false));

        bits[66] = true;
        Assert.AreEqual(66, bits.IndexOf(true));


        bits.Dispose();
        try
        {
            bits.IndexOf(true);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Insert()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        bits.Insert(88, true);
        Assert.IsTrue(bits[88]);
        Assert.AreEqual(101, bits.Count);

        bits.Insert(33, true);
        Assert.IsTrue(bits[33]);
        Assert.IsTrue(bits[89]);
        Assert.AreEqual(102, bits.Count);


        try
        {
            bits.Insert(-1, true);
            Assert.Fail();
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            bits.Insert(103, true);
            Assert.Fail();
        }
        catch (ArgumentException) { }


        var enumerator = bits.GetEnumerator();
        enumerator.MoveNext();
        bits.Insert(66, true);
        try
        {
            enumerator.MoveNext();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        bits.Dispose();
        try
        {
            bits.Insert(0, true);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void Remove()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        Assert.IsFalse(bits.Remove(true));
        Assert.AreEqual(100, bits.Count);

        Assert.IsTrue(bits.Remove(false));
        Assert.AreEqual(99, bits.Count);


        var enumerator = bits.GetEnumerator();
        enumerator.MoveNext();
        bits.Remove(false);
        try
        {
            enumerator.MoveNext();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        bits.Dispose();
        try
        {
            bits.Remove(false);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [TestMethod]
    public void RemoveAt()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        bits[33] = true;
        bits[88] = true;
        bits.RemoveAt(33);
        Assert.IsFalse(bits[33]);
        Assert.IsFalse(bits[88]);
        Assert.IsTrue(bits[87]);
        Assert.AreEqual(99, bits.Count);


        try
        {
            bits.RemoveAt(-1);
            Assert.Fail();
        }
        catch (ArgumentOutOfRangeException) { }
        try
        {
            bits.RemoveAt(100);
            Assert.Fail();
        }
        catch (ArgumentException) { }


        var enumerator = bits.GetEnumerator();
        enumerator.MoveNext();
        bits.RemoveAt(16);
        try
        {
            enumerator.MoveNext();
            Assert.Fail();
        }
        catch (InvalidOperationException) { }


        bits.Dispose();
        try
        {
            bits.RemoveAt(0);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }
}
