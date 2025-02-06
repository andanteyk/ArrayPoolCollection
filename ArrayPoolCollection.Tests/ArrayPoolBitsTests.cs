using System.Buffers;
using System.Collections;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolBitsTests
{
    [Fact]
    public void Items()
    {
        var rng = new Random(0);

        var bits = new ArrayPoolBits(200);
        for (int i = 0; i < 1024; i++)
        {
            int index = rng.Next(bits.Count);
            bool flag = rng.NextDouble() < 0.5;

            bits[index] = flag;
            Assert.Equal(flag, bits[index]);
        }

        Assert.Throws<IndexOutOfRangeException>(() => bits[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => bits[999] = true);


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits[0]);
        Assert.Throws<ObjectDisposedException>(() => bits[0] = true);
    }

    [Fact]
    public void Count()
    {
        var bits = new ArrayPoolBits();
        for (int i = 0; i < 1024; i++)
        {
            bits.Add(true);
            Assert.Equal(i + 1, bits.Count);
        }


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Count);
    }

    [Fact]
    public void Ctor()
    {
        using var empty = new ArrayPoolBits();
        Assert.Empty(empty);

        using var withLength = new ArrayPoolBits(123);
        Assert.Equal(123, withLength.Count);
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolBits(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolBits(int.MaxValue));

        using var withEnumerable = new ArrayPoolBits(Enumerable.Range(0, 200).Select(i => i % 2 != 0));
        Assert.Equal(200, withEnumerable.Count);
        for (int i = 0; i < 200; i++)
        {
            Assert.Equal(i % 2 != 0, withEnumerable[i]);
        }

        using var withBoolSpan = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 != 0).ToArray().AsSpan());
        Assert.Equal(100, withBoolSpan.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i % 3 != 0, withBoolSpan[i]);
        }

        using var withBoolArray = new ArrayPoolBits(Enumerable.Range(0, 300).Select(i => i % 3 == 0).ToArray().AsSpan());
        Assert.Equal(300, withBoolArray.Count);
        for (int i = 0; i < 300; i++)
        {
            Assert.Equal(i % 3 == 0, withBoolArray[i]);
        }

        using var withBytes = new ArrayPoolBits(MemoryMarshal.AsBytes<uint>(Enumerable.Repeat(0xaaaaaaaa, 10).ToArray()));
        Assert.Equal(4 * 10 * 8, withBytes.Count);
        for (int i = 0; i < 4 * 10 * 8; i++)
        {
            Assert.Equal(i % 2 != 0, withBytes[i]);
        }

        var clone = new ArrayPoolBits(withBoolArray);
        for (int i = 0; i < clone.Count; i++)
        {
            Assert.Equal(withBoolArray[i], clone[i]);
        }
        clone.Dispose();
        Assert.Throws<ObjectDisposedException>(() => new ArrayPoolBits(clone));
    }

    [Fact]
    public void Add()
    {
        var bits = new ArrayPoolBits();

        for (int i = 0; i < 1024; i++)
        {
            bits.Add(i % 2 != 0);
            Assert.Equal(i % 2 != 0, bits[i]);
            Assert.Equal(i + 1, bits.Count);
        }


        var enumerator = bits.GetEnumerator();
        bits.Add(false);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Add(false));
    }

    [Fact]
    public void And()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 == 0));

        left.And(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i % 6 == 0, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.And(right);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.Throws<ArgumentException>(() => left.And(right));


        right.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.And(right));

        left.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.And(right));
    }

    [Fact]
    public void AsSpan()
    {
        var bits = new ArrayPoolBits(200);
        var span = ArrayPoolBits.AsSpan(bits);

        Assert.Equal(new nuint[4], span.ToArray());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolBits.AsSpan(bits));
    }

    [Fact]
    public void Clear()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));

        bits.Clear();
        Assert.Empty(bits);


        var enumerator = bits.GetEnumerator();
        bits.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Clear());
    }

    [Fact]
    public void Contains()
    {
        var bits = new ArrayPoolBits();

        Assert.False(bits.Contains(false));
        Assert.False(bits.Contains(true));

        bits.Add(true);

        Assert.False(bits.Contains(false));
        Assert.True(bits.Contains(true));

        bits.Add(false);

        Assert.True(bits.Contains(false));
        Assert.True(bits.Contains(true));


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Contains(false));
    }

    [Fact]
    public void CopyTo()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var dest = new bool[128];

        bits.CopyTo(dest, 1);
        for (int i = 0; i <= 100; i++)
        {
            Assert.Equal(i % 2 != 0, dest[i]);
        }


        Assert.Throws<ArgumentOutOfRangeException>(() => bits.CopyTo(dest, -1));
        Assert.Throws<ArgumentException>(() => bits.CopyTo(dest, 99));


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.CopyTo(dest, 1));
    }

    [Fact]
    public void Dispose()
    {
        var bits = new ArrayPoolBits();

        bits.Dispose();
        bits.Dispose();
    }

    [Fact]
    public void GetEnumerator()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 != 0));

        int i = 0;
        foreach (var bit in bits)
        {
            Assert.Equal(i % 2 != 0, bit);
            i++;
        }

        var enumerator = bits.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        for (i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i % 2 != 0, enumerator.Current);
        }

        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
    }

    [Fact]
    public void HasAllSet()
    {
        var bits = new ArrayPoolBits(200);
        Assert.False(bits.HasAllSet());
        bits.Not();
        Assert.True(bits.HasAllSet());
        bits.Add(false);
        Assert.False(bits.HasAllSet());

        bits.Clear();
        Assert.True(bits.HasAllSet());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.HasAllSet());
    }

    [Fact]
    public void HasAnySet()
    {
        var bits = new ArrayPoolBits(200);
        Assert.False(bits.HasAnySet());
        bits.Not();
        Assert.True(bits.HasAnySet());
        bits.Add(false);
        Assert.True(bits.HasAnySet());

        bits.Clear();
        Assert.False(bits.HasAnySet());
        bits.Add(true);
        Assert.True(bits.HasAnySet());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.HasAnySet());
    }

    [Fact]
    public void IndexOf()
    {
        var bits = new ArrayPoolBits();
        Assert.Equal(-1, bits.IndexOf(false));
        Assert.Equal(-1, bits.IndexOf(true));

        bits.Add(false);
        Assert.Equal(0, bits.IndexOf(false));
        Assert.Equal(-1, bits.IndexOf(true));

        bits.Add(true);
        Assert.Equal(0, bits.IndexOf(false));
        Assert.Equal(1, bits.IndexOf(true));


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Add(false));
    }

    [Fact]
    public void Insert()
    {
        var bits = new ArrayPoolBits(127);
        bits.Insert(100, true);
        Assert.True(bits[100]);
        Assert.Equal(128, bits.Count);


        Assert.Throws<ArgumentOutOfRangeException>(() => bits.Insert(-1, true));
        Assert.Throws<ArgumentException>(() => bits.Insert(129, true));


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Insert(0, true));
    }

    [Fact]
    public void LeftShift()
    {
        var bits = new ArrayPoolBits(200);
        bits[100] = true;

        bits.LeftShift(70);
        Assert.True(bits[170]);

        bits[23] = true;
        bits.LeftShift(170);
        Assert.True(bits[193]);

        bits.LeftShift(9999);
        Assert.False(bits.HasAnySet());


        Assert.Throws<ArgumentOutOfRangeException>(() => bits.LeftShift(-1));


        var enumerator = bits.GetEnumerator();
        bits.LeftShift(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.LeftShift(1));
    }

    [Fact]
    public void Not()
    {
        var bits = new ArrayPoolBits(200);
        bits.Not();
        Assert.True(bits.HasAllSet());

        bits.Not();
        Assert.False(bits.HasAnySet());


        bits.Clear();
        bits.Not();


        bits.Add(false);
        var enumerator = bits.GetEnumerator();
        bits.Not();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Not());
    }

    [Fact]
    public void Or()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 != 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 != 0));

        left.Or(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i % 6 != 0, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.Or(right);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.Throws<ArgumentException>(() => left.Or(right));


        right.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.Or(right));

        left.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.Or(right));
    }

    [Fact]
    public void Remove()
    {
        var bits = new ArrayPoolBits(200);
        bits[123] = true;
        bits[12] = true;

        Assert.True(bits.Remove(true));
        Assert.False(bits[12]);
        Assert.True(bits[122]);

        Assert.True(bits.Remove(true));
        Assert.False(bits[122]);
        Assert.False(bits[123]);

        Assert.False(bits.Remove(true));

        Assert.True(bits.Remove(false));
        Assert.Equal(197, bits.Count);


        var enumerator = bits.GetEnumerator();
        bits.Remove(false);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.Remove(false));
    }

    [Fact]
    public void RemoveAt()
    {
        var bits = new ArrayPoolBits(200);
        bits[12] = true;
        bits[123] = true;

        bits.RemoveAt(12);
        Assert.False(bits[12]);
        Assert.False(bits[123]);
        Assert.True(bits[122]);

        bits.RemoveAt(122);
        Assert.False(bits.HasAnySet());


        Assert.Throws<ArgumentOutOfRangeException>(() => bits.RemoveAt(-1));
        Assert.Throws<ArgumentException>(() => bits.RemoveAt(999));


        var enumerator = bits.GetEnumerator();
        bits.RemoveAt(0);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.RemoveAt(0));
    }

    [Fact]
    public void RightShift()
    {
        var bits = new ArrayPoolBits(200);
        bits[100] = true;

        bits.RightShift(70);
        Assert.True(bits[30]);

        bits[193] = true;
        bits.RightShift(170);
        Assert.True(bits[23]);

        bits.RightShift(9999);
        Assert.False(bits.HasAnySet());


        Assert.Throws<ArgumentOutOfRangeException>(() => bits.RightShift(-1));


        var enumerator = bits.GetEnumerator();
        bits.RightShift(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.RightShift(1));
    }

    [Fact]
    public void SetAll()
    {
        var bits = new ArrayPoolBits(200);
        bits.SetAll(true);
        for (int i = 0; i < 200; i++)
        {
            Assert.True(bits[i]);
        }

        bits.SetAll(false);
        for (int i = 0; i < 200; i++)
        {
            Assert.False(bits[i]);
        }


        var enumerator = bits.GetEnumerator();
        bits.SetAll(true);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => bits.SetAll(false));
    }

    [Fact]
    public void SetCount()
    {
        var bits = new ArrayPoolBits(1000);

        ArrayPoolBits.SetCount(bits, 1024);
        Assert.Equal(1024, bits.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolBits.SetCount(bits, -1));
        Assert.Throws<ArgumentException>(() => ArrayPoolBits.SetCount(bits, 1025));


        var enumerator = bits.GetEnumerator();
        ArrayPoolBits.SetCount(bits, 13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolBits.SetCount(bits, 1));
    }

    [Fact]
    public void Xor()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 == 0));

        left.Xor(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal((i + 1) % 6 >= 3, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.Xor(right);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.Throws<ArgumentException>(() => left.Xor(right));


        right.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.Xor(right));

        left.Dispose();
        Assert.Throws<ObjectDisposedException>(() => left.Xor(right));
    }

    [Fact]
    public void Monkey()
    {
        var rng = new Random(0);

        var expect = new BitArray(1024);
        using var actual = new ArrayPoolBits(1024);

        for (int i = 0; i < 1024 * 1024; i++)
        {
            int index = rng.Next(1024);
            bool flag = rng.NextDouble() < 0.5;

            expect[index] = flag;
            actual[index] = flag;
        }

        Assert.Equal(expect.Cast<bool>(), actual);
    }

    // TODO
    //[ConditionalFact("HUGE")]
    public void Huge()
    {
        using var bits = new ArrayPoolBits(CollectionHelper.ArrayMaxLength);
        Assert.Equal(CollectionHelper.ArrayMaxLength, bits.Count);

        using var bits2 = new ArrayPoolBits(bits);

        bits[^1] = true;
        Assert.True(bits[^1]);

        Assert.Throws<OutOfMemoryException>(() => bits.Add(false));

        bits.And(bits2);

        Assert.Equal((CollectionHelper.ArrayMaxLength >> (UIntPtr.Size == 4 ? 5 : 6)) + 1, ArrayPoolBits.AsSpan(bits).Length);

        bits.Clear();
        ArrayPoolBits.SetCount(bits, CollectionHelper.ArrayMaxLength);

        Assert.False(bits.Contains(true));

        var buffer = new bool[CollectionHelper.ArrayMaxLength];
        bits.CopyTo(buffer, 0);

        foreach (var bit in bits)
        {
            Assert.False(bit);
        }

        Assert.False(bits.HasAllSet());
        Assert.False(bits.HasAnySet());

        Assert.Equal(-1, bits.IndexOf(true));

        Assert.Throws<OutOfMemoryException>(() => bits.Insert(0, false));

        bits.LeftShift(1);
        bits.RightShift(1);

        bits.Not();

        bits.Or(bits2);

        Assert.False(bits.Remove(false));

        bits.RemoveAt(0);
        bits.Insert(0, false);

        bits.SetAll(false);

        bits.Xor(bits2);
    }
}
