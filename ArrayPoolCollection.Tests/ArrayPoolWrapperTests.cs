using System.Collections;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolWrapperTests
{
    [TestMethod]
    public void Ctor()
    {
        // should not throw any exceptions
        using var one = new ArrayPoolWrapper<int>(1);
        using var zero = new ArrayPoolWrapper<int>(0);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolWrapper<int>(-1));
        Assert.ThrowsException<OutOfMemoryException>(() => new ArrayPoolWrapper<int>(int.MaxValue));

        Assert.AreEqual(0, one[0]);
    }

    [TestMethod]
    public void Items()
    {
        var array = new ArrayPoolWrapper<int>(6);

        array[3] = 123;
        Assert.AreEqual(123, array[3]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => array[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => array[123] = 1);

        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array[3]);
    }

    [TestMethod]
    public void Length()
    {
        var array = new ArrayPoolWrapper<int>(6);
        Assert.AreEqual(6, array.Length);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Length);
    }

    [TestMethod]
    public void LongLength()
    {
        var array = new ArrayPoolWrapper<int>(6);
        Assert.AreEqual(6L, array.LongLength);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.LongLength);
    }

    [TestMethod]
    public void AsReadOnly()
    {
        var array = new ArrayPoolWrapper<int>(6);
        array[3] = 123;

        var view = array.AsReadOnly();
        Assert.AreEqual(123, view[3]);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.AsReadOnly());
        Assert.ThrowsException<ObjectDisposedException>(() => view[3]);
    }

    [TestMethod]
    public void AsSpan()
    {
        var array = new ArrayPoolWrapper<int>(6);
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }


        Assert.AreEqual(6, array.AsSpan().Length);

        // implicit
        {
            Span<int> span = array;
            Assert.AreEqual(6, span.Length);
        }

        var fromOne = array.AsSpan(1);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5 }, fromOne.ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(8));

        var fromLastTwo = array.AsSpan(^2);
        CollectionAssert.AreEqual(new int[] { 4, 5 }, fromLastTwo.ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(^-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(^8));

        var centerTwo = array.AsSpan(2..4);
        CollectionAssert.AreEqual(new int[] { 2, 3 }, centerTwo.ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(-1..3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(1..-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(8..1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(1..8));

        var centerFour = array.AsSpan(1, 4);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, centerFour.ToArray());
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(1, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(8, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.AsSpan(1, 8));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.AsSpan());
    }

    [TestMethod]
    public void BinarySearch()
    {
        var array = Enumerable.Range(0, 64).Select(i => i * 2).ToArrayPool();

        Assert.AreEqual(4, array.BinarySearch(8));
        Assert.AreEqual(~4, array.BinarySearch(7));

        Assert.AreEqual(~10, array.BinarySearch(10, 10, 8));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.BinarySearch(8));
    }

    [TestMethod]
    public void Clear()
    {
        var array = new ArrayPoolWrapper<int>(8, false);
        array.Clear();
        CollectionAssert.AreEqual(new int[8], array);


        for (int i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        array.Clear(1, 3);
        CollectionAssert.AreEqual(new int[] { 0, 0, 0, 0, 4, 5, 6, 7 }, array);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Clear());
    }

    [TestMethod]
    public void Clone()
    {
        var source = Enumerable.Range(0, 100).ToArrayPool();
        using var clone = source.Clone();

        CollectionAssert.AreEqual(clone, source);


        source.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => source.Clone());
    }

    [TestMethod]
    public void ConvertAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        using var doubleArray = array.ConvertAll(i => i * 2.0);

        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Select(i => i * 2.0).ToArray(), doubleArray);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.ConvertAll(i => i * 2.0));
    }

    [TestMethod]
    public void Contains()
    {
        var array = new ArrayPoolWrapper<int>(6)
        {
            [4] = 123
        };

        Assert.IsTrue(array.Contains(123));
        Assert.IsFalse(array.Contains(456));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Contains(123));
    }

    [TestMethod]
    public void CopyTo()
    {
        var array = Enumerable.Range(1, 6).ToArrayPool();

        var dest = new int[10];
        array.CopyTo(dest, 2);
        CollectionAssert.AreEqual(new int[] { 0, 0, 1, 2, 3, 4, 5, 6, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6, 0, 0, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest.AsSpan(1..));
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 0, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest.AsMemory(3..));
        CollectionAssert.AreEqual(new int[] { 0, 0, 0, 1, 2, 3, 4, 5, 6, 0 }, dest);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.CopyTo(dest, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.CopyTo(dest, 11));
        Assert.ThrowsException<ArgumentException>(() => array.CopyTo(dest, 8));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.CopyTo(dest, 0));
    }

    [TestMethod]
    public void Dispose()
    {
        var array = new ArrayPoolWrapper<int>(6);
        array.Dispose();

        // should not throw any exceptions
        array.Dispose();
    }

    [TestMethod]
    public void Exists()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.IsTrue(array.Exists(i => i == 23));
        Assert.IsFalse(array.Exists(i => i == -1));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Exists(i => i == 1));
    }

    [TestMethod]
    public void Fill()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.Fill(123);
        CollectionAssert.AreEqual(Enumerable.Repeat(123, 100).ToArray(), array);


        array.Fill(456, 25, 50);
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Select(i => 25 <= i && i < 75 ? 456 : 123).ToArray(), array);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Fill(123));
    }

    [TestMethod]
    public void Find()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        Assert.AreEqual(12, array.Find(i => i == 12));
        Assert.AreEqual(default, array.Find(i => i < 0));

        using var stringArray = Enumerable.Repeat("hoge", 100).ToArrayPool();
        Assert.AreEqual(null, stringArray.Find(s => s == "fuga"));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Find(i => i == 10));
    }

    [TestMethod]
    public void FindAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        using var inRange = array.FindAll(i => 25 <= i && i < 75);
        CollectionAssert.AreEqual(Enumerable.Range(25, 50).ToArray(), inRange);

        using var empty = array.FindAll(i => i < 0);
        CollectionAssert.AreEqual(new int[0], empty);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.FindAll(i => i == 1));
    }

    [TestMethod]
    public void FindIndex()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.AreEqual(11, array.FindIndex(i => i % 12 == 11));
        Assert.AreEqual(23, array.FindIndex(20, i => i % 12 == 11));
        Assert.AreEqual(-1, array.FindIndex(12, 10, i => i % 12 == 11));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.FindIndex(i => i == 1));
    }

    [TestMethod]
    public void FindLast()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.AreEqual(95, array.FindLast(i => i % 12 == 11));
        Assert.AreEqual(default, array.FindLast(i => i < 0));

        var stringArray = Enumerable.Repeat("hoge", 100).ToArrayPool();
        Assert.AreEqual(null, stringArray.FindLast(s => s == "fuga"));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.FindLast(i => i == 0));
    }

    [TestMethod]
    public void FindLastIndex()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.AreEqual(95, array.FindLastIndex(i => i % 12 == 11));
        Assert.AreEqual(95, array.FindLastIndex(20, i => i % 12 == 11));
        Assert.AreEqual(-1, array.FindLastIndex(12, 10, i => i % 12 == 11));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.FindLastIndex(i => i == 1));
    }

    [TestMethod]
    public void ForEach()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.ForEach(i => Assert.IsTrue(i >= 0));
        array.ForEach((i, index) => Assert.AreEqual(index, i));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.ForEach(i => Assert.Fail()));
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        int expected = 0;
        foreach (var value in array)
        {
            Assert.AreEqual(expected, value);
            expected++;
        }


        var enumerator = array.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(i, enumerator.Current);
        }
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.IsTrue(enumerator.MoveNext());


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void IndexOf()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.AreEqual(12, array.IndexOf(12));
        Assert.AreEqual(-1, array.IndexOf(123));

        Assert.AreEqual(-1, array.IndexOf(12, 20));
        Assert.AreEqual(-1, array.IndexOf(48, 1, 16));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.IndexOf(12));
    }

    [TestMethod]
    public void LastIndexOf()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.AreEqual(12, array.LastIndexOf(12));
        Assert.AreEqual(-1, array.LastIndexOf(123));

        Assert.AreEqual(-1, array.LastIndexOf(12, 20));
        Assert.AreEqual(-1, array.LastIndexOf(48, 1, 16));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.LastIndexOf(12));
    }

    [TestMethod]
    public void Resize()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        ArrayPoolWrapper<int>.Resize(ref array, 200);
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Concat(Enumerable.Repeat(0, 100)).ToArray(), array);

        ArrayPoolWrapper<int>.Resize(ref array, 50);
        CollectionAssert.AreEqual(Enumerable.Range(0, 50).ToArray(), array);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ArrayPoolWrapper<int>.Resize(ref array, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ArrayPoolWrapper<int>.Resize(ref array, int.MaxValue));


        var enumerator = array.GetEnumerator();
        ArrayPoolWrapper<int>.Resize(ref array, 100);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolWrapper<int>.Resize(ref array, 123));
    }

    [TestMethod]
    public void Reverse()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.Reverse();
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Reverse().ToArray(), array);

        array.Reverse();
        array.Reverse(12, 34);

        CollectionAssert.AreEqual(
            Enumerable.Range(0, 12).Concat(Enumerable.Range(12, 34).Reverse()).Concat(Enumerable.Range(46, 54)).ToArray(),
            array);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Reverse(-1, 12));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Reverse(12, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Reverse(12, 123));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Reverse());
    }

    [TestMethod]
    public void Slice()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        CollectionAssert.AreEqual(Enumerable.Range(12, 34).ToArray(), array.Slice(12, 34).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(56, 22).ToArray(), array[56..78].ToArray());

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Slice(-1, 12));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Slice(12, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Slice(12, 123));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Slice(12, 34));
    }

    [TestMethod]
    public void Sort()
    {
        var rng = new Random(0);
        var array = Enumerable.Range(0, 100).ToArrayPool();

        static void Shuffle<T>(Span<T> span, Random rng)
        {
            for (int i = 1; i < span.Length; i++)
            {
                int r = rng.Next(i + 1);
                (span[i], span[r]) = (span[r], span[i]);
            }
        }

        Shuffle(array.AsSpan(), rng);
        array.Sort();
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).ToArray(), array);

        Shuffle(array.AsSpan(), rng);
        array.Sort((a, b) => b - a);
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Reverse().ToArray(), array);

        array.Sort(25, 50);
        CollectionAssert.AreEqual(Enumerable.Range(75, 25).Reverse().Concat(Enumerable.Range(25, 50)).Concat(Enumerable.Range(0, 25).Reverse()).ToArray(), array);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Sort(-1, 12));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Sort(12, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => array.Sort(12, 123));


        using ArrayPoolWrapper<string> stringArray = ["Alice", "abigail", "Barbara", "Charlotte"];
        Shuffle(stringArray.AsSpan(), rng);
        stringArray.Sort(StringComparer.OrdinalIgnoreCase);
        CollectionAssert.AreEqual(new string[] { "abigail", "Alice", "Barbara", "Charlotte" }, stringArray);


        array.Sort();
        var keyArray = Enumerable.Range(0, 100).Reverse().ToArrayPool();
        ArrayPoolWrapper<int>.Sort(keyArray, array);
        CollectionAssert.AreEqual(Enumerable.Range(0, 100).Reverse().ToArray(), array);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.Sort());
    }

    [TestMethod]
    public void TrueForAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.IsTrue(array.TrueForAll(i => i >= 0));
        Assert.IsFalse(array.TrueForAll(i => i == 12));


        using var empty = new ArrayPoolWrapper<int>(0);
        Assert.IsTrue(empty.TrueForAll(i => false));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.TrueForAll(i => true));
    }

    [TestMethod]
    public void Create()
    {
        using ArrayPoolWrapper<int> array = [2, 3, 5, 7, 11, 13, 17, 19];
        CollectionAssert.AreEqual(new int[] { 2, 3, 5, 7, 11, 13, 17, 19 }, array);
    }

    [TestMethod]
    public void Contains_ICollectionT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection<int> collection = array;

        Assert.IsTrue(collection.Contains(12));
        Assert.IsFalse(collection.Contains(-1));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => collection.Contains(12));
    }

    [TestMethod]
    public void CopyTo_ICollectionT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection<int> collection = array;

        var buffer = new int[128];
        collection.CopyTo(buffer, 10);
        CollectionAssert.AreEqual(Enumerable.Repeat(0, 10).Concat(Enumerable.Range(0, 100)).Concat(Enumerable.Repeat(0, 18)).ToArray(), buffer);


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => collection.CopyTo(buffer, 0));
    }

    [TestMethod]
    public void IndexOf_IListT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList<int> list = array;

        Assert.AreEqual(12, list.IndexOf(12));
        Assert.AreEqual(-1, list.IndexOf(-1));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.IndexOf(12));
    }

    [TestMethod]
    public void Contains_IList()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList list = array;

        Assert.IsTrue(list.Contains(12));
        Assert.IsFalse(list.Contains(-1));
        Assert.IsFalse(list.Contains("hoge"));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Contains(12));

    }

    [TestMethod]
    public void IndexOf_IList()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList list = array;

        Assert.AreEqual(12, list.IndexOf(12));
        Assert.AreEqual(-1, list.IndexOf(-1));
        Assert.AreEqual(-1, list.IndexOf("hoge"));


        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.IndexOf(12));

    }

    [TestMethod]
    public void CopyTo_ICollection()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection collection = array;

        var buffer = new int[128];
        collection.CopyTo(buffer, 10);
        CollectionAssert.AreEqual(Enumerable.Repeat(0, 10).Concat(Enumerable.Range(0, 100)).Concat(Enumerable.Repeat(0, 18)).ToArray(), buffer);

        var stringBuffer = new string[128];
        Assert.ThrowsException<ArgumentException>(() => collection.CopyTo(stringBuffer, 0));

        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => collection.CopyTo(buffer, 0));

    }
}
