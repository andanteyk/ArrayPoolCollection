using System.Buffers;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolBitsTests
{
    [TestMethod]
    public void Items()
    {
        var rng = new Random(0);

        var bits = new ArrayPoolBits(200);
        for (int i = 0; i < 1024; i++)
        {
            int index = rng.Next(bits.Count);
            bool flag = rng.NextDouble() < 0.5;

            bits[index] = flag;
            Assert.AreEqual(flag, bits[index]);
        }


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits[0]);
    }

    [TestMethod]
    public void Count()
    {
        var bits = new ArrayPoolBits();
        for (int i = 0; i < 1024; i++)
        {
            bits.Add(true);
            Assert.AreEqual(i + 1, bits.Count);
        }
    }

    [TestMethod]
    public void Ctor()
    {
        using var empty = new ArrayPoolBits();
        Assert.AreEqual(0, empty.Count);

        using var withLength = new ArrayPoolBits(123);
        Assert.AreEqual(123, withLength.Count);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolBits(-1));

        using var withEnumerable = new ArrayPoolBits(Enumerable.Range(0, 200).Select(i => i % 2 != 0));
        Assert.AreEqual(200, withEnumerable.Count);
        for (int i = 0; i < 200; i++)
        {
            Assert.AreEqual(i % 2 != 0, withEnumerable[i]);
        }

        using var withBoolSpan = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 != 0).ToArray().AsSpan());
        Assert.AreEqual(100, withBoolSpan.Count);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i % 3 != 0, withBoolSpan[i]);
        }

        using var withBoolArray = new ArrayPoolBits(Enumerable.Range(0, 300).Select(i => i % 3 == 0).ToArray().AsSpan());
        Assert.AreEqual(300, withBoolArray.Count);
        for (int i = 0; i < 300; i++)
        {
            Assert.AreEqual(i % 3 == 0, withBoolArray[i]);
        }

        using var withBytes = new ArrayPoolBits(MemoryMarshal.AsBytes<uint>(Enumerable.Repeat(0xaaaaaaaa, 10).ToArray()));
        Assert.AreEqual(4 * 10 * 8, withBytes.Count);
        for (int i = 0; i < 4 * 10 * 8; i++)
        {
            Assert.AreEqual(i % 2 != 0, withBytes[i]);
        }

        var clone = new ArrayPoolBits(withBoolArray);
        for (int i = 0; i < clone.Count; i++)
        {
            Assert.AreEqual(withBoolArray[i], clone[i]);
        }
        clone.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => new ArrayPoolBits(clone));
    }

    [TestMethod]
    public void Add()
    {
        var bits = new ArrayPoolBits();

        for (int i = 0; i < 1024; i++)
        {
            bits.Add(i % 2 != 0);
            Assert.AreEqual(i % 2 != 0, bits[i]);
            Assert.AreEqual(i + 1, bits.Count);
        }


        var enumerator = bits.GetEnumerator();
        bits.Add(false);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Add(false));
    }

    [TestMethod]
    public void And()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 == 0));

        left.And(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i % 6 == 0, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.And(right);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.ThrowsException<ArgumentException>(() => left.And(right));


        right.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.And(right));

        left.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.And(right));
    }

    [TestMethod]
    public void AsSpan()
    {
        var bits = new ArrayPoolBits(200);
        var span = ArrayPoolBits.AsSpan(bits);

        CollectionAssert.AreEqual(new nuint[4], span.ToArray());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolBits.AsSpan(bits));
    }

    [TestMethod]
    public void Clear()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));

        bits.Clear();
        Assert.AreEqual(0, bits.Count);


        var enumerator = bits.GetEnumerator();
        bits.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Clear());
    }

    [TestMethod]
    public void Contains()
    {
        var bits = new ArrayPoolBits();

        Assert.IsFalse(bits.Contains(false));
        Assert.IsFalse(bits.Contains(true));

        bits.Add(true);

        Assert.IsFalse(bits.Contains(false));
        Assert.IsTrue(bits.Contains(true));

        bits.Add(false);

        Assert.IsTrue(bits.Contains(false));
        Assert.IsTrue(bits.Contains(true));


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Contains(false));
    }

    [TestMethod]
    public void CopyTo()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var dest = new bool[128];

        bits.CopyTo(dest, 1);
        for (int i = 0; i <= 100; i++)
        {
            Assert.AreEqual(i % 2 != 0, dest[i]);
        }


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits.CopyTo(dest, -1));
        Assert.ThrowsException<ArgumentException>(() => bits.CopyTo(dest, 99));


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.CopyTo(dest, 1));
    }

    [TestMethod]
    public void Dispose()
    {
        var bits = new ArrayPoolBits();

        bits.Dispose();
        bits.Dispose();
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var bits = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 != 0));

        int i = 0;
        foreach (var bit in bits)
        {
            Assert.AreEqual(i % 2 != 0, bit);
            i++;
        }

        var enumerator = bits.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

        for (i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(i % 2 != 0, enumerator.Current);
        }

        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Reset());
    }

    [TestMethod]
    public void HasAllSet()
    {
        var bits = new ArrayPoolBits(200);
        Assert.IsFalse(bits.HasAllSet());
        bits.Not();
        Assert.IsTrue(bits.HasAllSet());
        bits.Add(false);
        Assert.IsFalse(bits.HasAllSet());

        bits.Clear();
        Assert.IsTrue(bits.HasAllSet());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.HasAllSet());
    }

    [TestMethod]
    public void HasAnySet()
    {
        var bits = new ArrayPoolBits(200);
        Assert.IsFalse(bits.HasAnySet());
        bits.Not();
        Assert.IsTrue(bits.HasAnySet());
        bits.Add(false);
        Assert.IsTrue(bits.HasAnySet());

        bits.Clear();
        Assert.IsFalse(bits.HasAnySet());
        bits.Add(true);
        Assert.IsTrue(bits.HasAnySet());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.HasAnySet());
    }

    [TestMethod]
    public void IndexOf()
    {
        var bits = new ArrayPoolBits();
        Assert.AreEqual(-1, bits.IndexOf(false));
        Assert.AreEqual(-1, bits.IndexOf(true));

        bits.Add(false);
        Assert.AreEqual(0, bits.IndexOf(false));
        Assert.AreEqual(-1, bits.IndexOf(true));

        bits.Add(true);
        Assert.AreEqual(0, bits.IndexOf(false));
        Assert.AreEqual(1, bits.IndexOf(true));


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Add(false));
    }

    [TestMethod]
    public void Insert()
    {
        var bits = new ArrayPoolBits(127);
        bits.Insert(100, true);
        Assert.IsTrue(bits[100]);
        Assert.AreEqual(128, bits.Count);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits.Insert(-1, true));
        Assert.ThrowsException<ArgumentException>(() => bits.Insert(129, true));


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Insert(0, true));
    }

    [TestMethod]
    public void LeftShift()
    {
        var bits = new ArrayPoolBits(200);
        bits[100] = true;

        bits.LeftShift(70);
        Assert.IsTrue(bits[170]);

        bits[23] = true;
        bits.LeftShift(170);
        Assert.IsTrue(bits[193]);

        bits.LeftShift(9999);
        Assert.IsFalse(bits.HasAnySet());


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits.LeftShift(-1));


        var enumerator = bits.GetEnumerator();
        bits.LeftShift(1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.LeftShift(1));
    }

    public void Not()
    {
        var bits = new ArrayPoolBits(200);
        bits.Not();
        Assert.IsTrue(bits.HasAllSet());

        bits.Not();
        Assert.IsFalse(bits.HasAnySet());


        bits.Clear();
        bits.Not();


        bits.Add(false);
        var enumerator = bits.GetEnumerator();
        bits.Not();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Not());
    }

    public void Or()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 != 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 != 0));

        left.Or(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i % 6 != 0, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.Or(right);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.ThrowsException<ArgumentException>(() => left.Or(right));


        right.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.Or(right));

        left.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.Or(right));
    }

    public void Remove()
    {
        var bits = new ArrayPoolBits(200);
        bits[123] = true;
        bits[12] = true;

        Assert.IsTrue(bits.Remove(true));
        Assert.IsFalse(bits[12]);
        Assert.IsTrue(bits[122]);

        Assert.IsTrue(bits.Remove(true));
        Assert.IsFalse(bits[122]);
        Assert.IsFalse(bits[123]);

        Assert.IsFalse(bits.Remove(true));

        Assert.IsTrue(bits.Remove(false));
        Assert.AreEqual(197, bits.Count);


        var enumerator = bits.GetEnumerator();
        bits.Remove(false);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.Remove(false));
    }

    [TestMethod]
    public void RemoveAt()
    {
        var bits = new ArrayPoolBits(200);
        bits[12] = true;
        bits[123] = true;

        bits.RemoveAt(12);
        Assert.IsFalse(bits[12]);
        Assert.IsFalse(bits[123]);
        Assert.IsTrue(bits[122]);

        bits.RemoveAt(122);
        Assert.IsFalse(bits.HasAnySet());


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits.RemoveAt(-1));
        Assert.ThrowsException<ArgumentException>(() => bits.RemoveAt(999));


        var enumerator = bits.GetEnumerator();
        bits.RemoveAt(0);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.RemoveAt(0));
    }

    [TestMethod]
    public void RightShift()
    {
        var bits = new ArrayPoolBits(200);
        bits[100] = true;

        bits.RightShift(70);
        Assert.IsTrue(bits[30]);

        bits[193] = true;
        bits.RightShift(170);
        Assert.IsTrue(bits[23]);

        bits.RightShift(9999);
        Assert.IsFalse(bits.HasAnySet());


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => bits.RightShift(-1));


        var enumerator = bits.GetEnumerator();
        bits.RightShift(1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.RightShift(1));
    }

    [TestMethod]
    public void SetAll()
    {
        var bits = new ArrayPoolBits(200);
        bits.SetAll(true);
        for (int i = 0; i < 200; i++)
        {
            Assert.IsTrue(bits[i]);
        }

        bits.SetAll(false);
        for (int i = 0; i < 200; i++)
        {
            Assert.IsFalse(bits[i]);
        }


        var enumerator = bits.GetEnumerator();
        bits.SetAll(true);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => bits.SetAll(false));
    }

    [TestMethod]
    public void SetCount()
    {
        var bits = new ArrayPoolBits(1000);

        ArrayPoolBits.SetCount(bits, 1024);
        Assert.AreEqual(1024, bits.Count);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ArrayPoolBits.SetCount(bits, -1));
        Assert.ThrowsException<ArgumentException>(() => ArrayPoolBits.SetCount(bits, 1025));


        var enumerator = bits.GetEnumerator();
        ArrayPoolBits.SetCount(bits, 13);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        bits.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolBits.SetCount(bits, 1));
    }

    [TestMethod]
    public void Xor()
    {
        var left = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 2 == 0));
        var right = new ArrayPoolBits(Enumerable.Range(0, 100).Select(i => i % 3 == 0));

        left.Xor(right);
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual((i + 1) % 6 >= 3, left[i]);
        }


        var enumerator = left.GetEnumerator();
        left.Xor(right);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        left.Add(false);
        Assert.ThrowsException<ArgumentException>(() => left.Xor(right));


        right.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.Xor(right));

        left.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => left.Xor(right));
    }
}
