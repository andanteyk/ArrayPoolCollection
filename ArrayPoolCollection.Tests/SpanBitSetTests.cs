namespace ArrayPoolCollection.Tests;

public class SpanBitSetTests
{
    [Fact]
    public void Items()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 128);

        for (int i = 0; i < 128; i++)
        {
            bits[i] = i % 2 == 1;
        }

        for (int i = 0; i < 128; i++)
        {
            Assert.Equal(i % 2 == 1, bits[i]);
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

    [Fact]
    public void Count()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 128);

        Assert.Equal(128, bits.Count);

        bits.Add(false);
        Assert.Equal(129, bits.Count);

        bits.RemoveAt(0);
        Assert.Equal(128, bits.Count);


        bits.Dispose();
        try
        {
            _ = bits.Count;
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [Fact]
    public void Ctor()
    {
        using var fromSpanLength = new SpanBitSet(stackalloc nuint[16], 150);
        Assert.Equal(150, fromSpanLength.Count);

        using var fromCapacityLength = new SpanBitSet(1024, 150);
        Assert.Equal(150, fromCapacityLength.Count);

        using var fromSpanOverLength = new SpanBitSet(stackalloc nuint[16], 32 * 16);
        Assert.Equal(32 * 16, fromSpanOverLength.Count);
    }

    [Fact]
    public void Add()
    {
        var bits = new SpanBitSet(stackalloc nuint[1], 0);

        for (int i = 0; i < 1024; i++)
        {
            bits.Add(i % 2 == 1);
            Assert.Equal(i % 2 == 1, bits[i]);
            Assert.Equal(i + 1, bits.Count);
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

    [Fact]
    public void Clear()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);
        bits.Clear();
        Assert.Equal(0, bits.Count);


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

    [Fact]
    public void Contains()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);
        Assert.True(bits.Contains(false));
        Assert.False(bits.Contains(true));

        bits[0] = true;

        Assert.True(bits.Contains(false));
        Assert.True(bits.Contains(true));


        bits.Dispose();
        try
        {
            bits.Contains(false);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [Fact]
    public void CopyTo()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);
        bits[0] = true;
        bits[99] = true;

        var bools = new bool[128];

        bits.CopyTo(bools, 0);
        Assert.True(bools[0]);
        Assert.True(bools[99]);

        bits.CopyTo(bools.AsSpan(1..));
        Assert.True(bools[1]);
        Assert.False(bools[99]);
        Assert.True(bools[100]);

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

    [Fact]
    public void Dispose()
    {
        using var fromSpan = new SpanBitSet(stackalloc nuint[16], 0);

        using var fromArray = new SpanBitSet(1024, 0);
    }

    [Fact]
    public void GetEnumerator()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 64);

        int i = 0;
        foreach (var b in bits)
        {
            i++;
        }
        Assert.Equal(64, i);


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

    [Fact]
    public void IndexOf()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        Assert.Equal(-1, bits.IndexOf(true));
        Assert.Equal(0, bits.IndexOf(false));

        bits[66] = true;
        Assert.Equal(66, bits.IndexOf(true));


        bits.Dispose();
        try
        {
            bits.IndexOf(true);
            Assert.Fail();
        }
        catch (ObjectDisposedException) { }
    }

    [Fact]
    public void Insert()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        bits.Insert(88, true);
        Assert.True(bits[88]);
        Assert.Equal(101, bits.Count);

        bits.Insert(33, true);
        Assert.True(bits[33]);
        Assert.True(bits[89]);
        Assert.Equal(102, bits.Count);


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

    [Fact]
    public void Remove()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        Assert.False(bits.Remove(true));
        Assert.Equal(100, bits.Count);

        Assert.True(bits.Remove(false));
        Assert.Equal(99, bits.Count);


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

    [Fact]
    public void RemoveAt()
    {
        var bits = new SpanBitSet(stackalloc nuint[16], 100);

        bits[33] = true;
        bits[88] = true;
        bits.RemoveAt(33);
        Assert.False(bits[33]);
        Assert.False(bits[88]);
        Assert.True(bits[87]);
        Assert.Equal(99, bits.Count);


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
