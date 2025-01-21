using System.Collections;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolListTests
{
    [TestMethod]
    public void Ctor()
    {
        // should not throw any exceptions
        using var defaultList = new ArrayPoolList<int>();

        // should not throw any exceptions
        using var emptyList = new ArrayPoolList<int>(0);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolList<int>(-1));

        using var enumerableList = new ArrayPoolList<int>(Enumerable.Range(0, 24));
        CollectionAssert.AreEqual(Enumerable.Range(0, 24).ToList(), enumerableList);
    }

    [TestMethod]
    public void Capacity()
    {
        var list = new ArrayPoolList<int>(32);
        Assert.AreEqual(32, list.Capacity);

        list.Capacity = 48;
        Assert.AreEqual(64, list.Capacity);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Capacity = -1);

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Capacity);
    }

    [TestMethod]
    public void Items()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.AreEqual(1, list[0]);
        list[0] = 123;
        Assert.AreEqual(123, list[0]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => list[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => list[4] = -4);

        var ilist = (IList)list;
        Assert.AreEqual(123, ilist[0]);
        ilist[0] = 456;
        Assert.AreEqual(456, ilist[0]);
        Assert.ThrowsException<ArgumentException>(() => ilist[0] = "hoge");

        Assert.ThrowsException<IndexOutOfRangeException>(() => ilist[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => ilist[4] = -4);

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        var ilistString = (IList)stringList;
        ilistString[0] = null;
        Assert.AreEqual(null, ilistString[0]);
        Assert.ThrowsException<ArgumentException>(() => ilistString[0] = 123);

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list[0]);
        Assert.ThrowsException<ObjectDisposedException>(() => list[0] = -1);
    }

    [TestMethod]
    public void Count()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.AreEqual(3, list.Count);
        list.Add(4);
        Assert.AreEqual(4, list.Count);

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Count);
    }

    [TestMethod]
    public void Add()
    {
        var list = new ArrayPoolList<int>();
        list.Add(1);

        Assert.AreEqual(1, list[0]);
        Assert.AreEqual(1, list.Count);

        list.Add(2);
        Assert.AreEqual(2, list[1]);
        Assert.AreEqual(2, list.Count);

        for (int i = 0; i < 98; i++)
        {
            list.Add(i);
        }
        Assert.AreEqual(100, list.Count);


        var enumerator = list.GetEnumerator();
        list.Add(-1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Add(-2));
    }

    [TestMethod]
    public void AddRange()
    {
        var list = new ArrayPoolList<int>();

        // TryGetNonEnumeratedCount == true, but not ICollection<T>
        list.AddRange(Enumerable.Range(0, 16));
        CollectionAssert.AreEqual(Enumerable.Range(0, 16).ToArray(), list);

        // TryGetNonEnumeratedCount == true, ICollection<T>
        list.AddRange(Enumerable.Range(16, 16).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(0, 32).ToArray(), list);

        static IEnumerable<int> Iter()
        {
            for (int i = 32; i < 48; i++)
            {
                yield return i;
            }
        }

        // TryGetNonEnumeratedCount == false
        list.AddRange(Iter());
        CollectionAssert.AreEqual(Enumerable.Range(0, 48).ToArray(), list);

        list.AddRange([]);
        CollectionAssert.AreEqual(Enumerable.Range(0, 48).ToArray(), list);


        var enumerator = list.GetEnumerator();
        list.AddRange(Enumerable.Range(0, 16));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.AddRange(Enumerable.Range(0, 16)));
    }

    [TestMethod]
    public void AddRangeFromSpan()
    {
        var list = new ArrayPoolList<int>();

        list.AddRangeFromSpan(Enumerable.Range(0, 16).ToArray());
        CollectionAssert.AreEqual(Enumerable.Range(0, 16).ToArray(), list);

        list.AddRangeFromSpan([]);
        CollectionAssert.AreEqual(Enumerable.Range(0, 16).ToArray(), list);


        var enumerator = list.GetEnumerator();
        list.AddRangeFromSpan(Enumerable.Range(0, 16).ToArray());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.AddRangeFromSpan(Enumerable.Range(0, 16).ToArray()));
    }

    [TestMethod]
    public void AsReadOnly()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        var readonlyList = list.AsReadOnly();

        CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, readonlyList);

        list.Add(4);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4 }, readonlyList);


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.AsReadOnly());
        Assert.ThrowsException<ObjectDisposedException>(() => readonlyList[0]);
    }

    [TestMethod]
    public void BinarySearch()
    {
        var list = new ArrayPoolList<int>() { 2, 4, 6, 8, 10 };

        Assert.AreEqual(0, list.BinarySearch(2));
        Assert.AreEqual(2, list.BinarySearch(6));
        Assert.AreEqual(~0, list.BinarySearch(1));
        Assert.AreEqual(~3, list.BinarySearch(7));
        Assert.AreEqual(~5, list.BinarySearch(11));

        Assert.AreEqual(1, list.BinarySearch(4, Comparer<int>.Create((a, b) => a - b)));
        Assert.ThrowsException<ArgumentNullException>(() => list.BinarySearch(8, null!));

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.BinarySearch(2));
    }

    [TestMethod]
    public void Clear()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        list.Clear();
        Assert.AreEqual(0, list.Count);

        list.Clear();
        Assert.AreEqual(0, list.Count);


        var enumerator = list.GetEnumerator();
        list.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Clear());
    }

    [TestMethod]
    public void Contains()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.IsTrue(list.Contains(1));
        Assert.IsFalse(list.Contains(-1));


        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.IsTrue(stringList.Contains("hoge"));
        Assert.IsFalse(stringList.Contains(null!));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Contains(1));
    }

    [TestMethod]
    public void ConvertAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        using var doubleList = list.ConvertAll(i => (double)i);
        CollectionAssert.AreEqual(new double[] { 1.0, 2.0, 3.0 }, doubleList);


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.ConvertAll(i => (double)i));
    }

    [TestMethod]
    public void CopyTo()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var buffer = new int[8];

        list.CopyTo(2, buffer, 3, 4);
        CollectionAssert.AreEqual(new int[] { 0, 0, 0, 3, 4, 5, 6, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6, 0, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer, 1);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer.AsSpan(1..7));
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, buffer);


        Assert.ThrowsException<ArgumentNullException>(() => list.CopyTo(null!));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.CopyTo(-1, buffer, 0, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.CopyTo(0, buffer, -1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.CopyTo(0, buffer, 0, -1));
        Assert.ThrowsException<ArgumentException>(() => list.CopyTo(10, buffer, 0, 1));
        Assert.ThrowsException<ArgumentException>(() => list.CopyTo(1, buffer, 7, 8));
        Assert.ThrowsException<ArgumentException>(() => list.CopyTo(buffer.AsSpan(1..2)));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.CopyTo(buffer));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var list = new ArrayPoolList<int>();
        Assert.AreEqual(64, list.EnsureCapacity(48));
        Assert.AreEqual(64, list.Capacity);

        Assert.AreEqual(64, list.EnsureCapacity(16));
        Assert.AreEqual(64, list.Capacity);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.EnsureCapacity(-1));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.EnsureCapacity(64));
    }

    [TestMethod]
    public void Exists()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        Assert.IsTrue(list.Exists(i => i == 2));
        Assert.IsTrue(list.Exists(i => i < 4));
        Assert.IsFalse(list.Exists(i => i == 4));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Exists(i => i == 2));
    }

    [TestMethod]
    public void Find()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        Assert.AreEqual(2, list.Find(i => i == 2));
        Assert.AreEqual(1, list.Find(i => i < 4));
        Assert.AreEqual(0, list.Find(i => i == 4));

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.AreEqual("hoge", stringList.Find(s => s.Length < 5));
        Assert.AreEqual(null, stringList.Find(s => s.Length >= 5));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Find(i => i == 2));
    }

    [TestMethod]
    public void FindAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        CollectionAssert.AreEqual(new int[] { 2, 4, 6 }, list.FindAll(i => i % 2 == 0));
        CollectionAssert.AreEqual(new int[0], list.FindAll(i => i > 10));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.FindAll(i => i == 2));
    }

    [TestMethod]
    public void FindIndex()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.AreEqual(2, list.FindIndex(i => i == 3));
        Assert.AreEqual(1, list.FindIndex(i => i % 2 == 0));
        Assert.AreEqual(-1, list.FindIndex(i => i == 10));

        Assert.AreEqual(2, list.FindIndex(1, i => i % 2 == 1));
        Assert.AreEqual(2, list.FindIndex(1, 3, i => i % 2 == 1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindIndex(-1, i => i == 3));
        Assert.ThrowsException<ArgumentException>(() => list.FindIndex(10, i => i == 3));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindIndex(-1, 1, i => i == 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindIndex(1, -1, i => i == 3));
        Assert.ThrowsException<ArgumentException>(() => list.FindIndex(1, 10, i => i == 3));
        Assert.ThrowsException<ArgumentException>(() => list.FindIndex(10, 1, i => i == 3));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.FindIndex(i => i == 3));
    }

    [TestMethod]
    public void FindLast()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.AreEqual(5, list.FindLast(i => i % 2 == 1));
        Assert.AreEqual(0, list.FindLast(i => i > 10));

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.AreEqual("piyo", stringList.FindLast(s => s.Length < 5));
        Assert.AreEqual(null, stringList.FindLast(s => s.Length >= 5));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.FindLast(i => i == 3));
    }


    [TestMethod]
    public void FindLastIndex()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.AreEqual(2, list.FindLastIndex(i => i == 3));
        Assert.AreEqual(5, list.FindLastIndex(i => i % 2 == 0));
        Assert.AreEqual(-1, list.FindLastIndex(i => i == 10));

        Assert.AreEqual(4, list.FindLastIndex(1, i => i % 2 == 1));
        Assert.AreEqual(2, list.FindLastIndex(1, 3, i => i % 2 == 1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, i => i == 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(10, i => i == 3));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, 1, i => i == 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(1, -1, i => i == 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(1, 10, i => i == 3));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.FindLastIndex(10, 1, i => i == 3));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.FindLastIndex(i => i == 3));
    }

    [TestMethod]
    public void ForEach()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        using var list2 = new ArrayPoolList<double>();

        list.ForEach(i => list2.Add(i * 2.0));
        CollectionAssert.AreEqual(new double[] { 2.0, 4.0, 6.0, 8.0, 10.0, 12.0 }, list2);

        Assert.ThrowsException<InvalidOperationException>(() => list.ForEach(i => list.Add(i * 2)));
        Assert.ThrowsException<InvalidOperationException>(() => list.ForEach(i => list.Remove(i)));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.ForEach(i => list2.Add(i * 2.0)));
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        {
            using var enumerator = list.GetEnumerator();
            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

            for (int i = 0; enumerator.MoveNext(); i++)
            {
                Assert.AreEqual(i + 1, enumerator.Current);
            }

            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
            enumerator.MoveNext();
            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        }

        {
            using var enumerator = list.GetEnumerator();
            enumerator.MoveNext();
            list.Add(7);
            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        }

        {
            using var enumerator = list.GetEnumerator();
            enumerator.MoveNext();
            list.Remove(7);
            Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        }


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.GetEnumerator());
    }

    [TestMethod]
    public void GetRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        using var ranged = list.GetRange(1, 3);
        CollectionAssert.AreEqual(new int[] { 2, 3, 4 }, ranged);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.GetRange(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.GetRange(1, -1));
        Assert.ThrowsException<ArgumentException>(() => list.GetRange(1, 7));
        Assert.ThrowsException<ArgumentException>(() => list.GetRange(7, 1));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.GetRange(1, 3));
    }

    [TestMethod]
    public void IndexOf()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        Assert.AreEqual(2, list.IndexOf(3));
        Assert.AreEqual(-1, list.IndexOf(7));
        Assert.AreEqual(-1, list.IndexOf(3, 4));
        Assert.AreEqual(2, list.IndexOf(3, 2, 4));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, 7));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, -1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, 1, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, 7, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.IndexOf(3, 1, 7));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.IndexOf(1));
    }

    [TestMethod]
    public void Insert()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.Insert(1, 9);
        CollectionAssert.AreEqual(new int[] { 1, 9, 2, 3, 4, 5, 6 }, list);

        list.Insert(7, 8);
        CollectionAssert.AreEqual(new int[] { 1, 9, 2, 3, 4, 5, 6, 8 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Insert(-1, 99));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Insert(10, 99));


        var enumerator = list.GetEnumerator();
        list.Insert(1, 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Insert(1, 10));
    }

    [TestMethod]
    public void InsertRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        // TryNonEnumeratedCount == true, but not ICollection<T>
        list.InsertRange(1, Enumerable.Range(10, 3));
        CollectionAssert.AreEqual(new int[] { 1, 10, 11, 12, 2, 3, 4, 5, 6 }, list);

        // TryNonEnumeratedCount == true, and also ICollection<T>
        list.InsertRange(2, Enumerable.Range(20, 3).ToArray());
        CollectionAssert.AreEqual(new int[] { 1, 10, 20, 21, 22, 11, 12, 2, 3, 4, 5, 6 }, list);

        static IEnumerable<int> Ints()
        {
            yield return 30;
            yield return 31;
            yield return 32;
        }

        // TryNonEnumeratedCount == false
        list.InsertRange(3, Ints());
        CollectionAssert.AreEqual(new int[] { 1, 10, 20, 30, 31, 32, 21, 22, 11, 12, 2, 3, 4, 5, 6 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.InsertRange(-1, [99]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.InsertRange(99, [99]));


        var enumerator = list.GetEnumerator();
        list.InsertRange(1, [10]);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.InsertRange(1, [10]));
    }

    [TestMethod]
    public void InsertRangeFromSpan()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.InsertRangeFromSpan(1, Enumerable.Range(10, 3).ToArray());
        CollectionAssert.AreEqual(new int[] { 1, 10, 11, 12, 2, 3, 4, 5, 6 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.InsertRangeFromSpan(-1, [99]));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.InsertRangeFromSpan(99, [99]));


        var enumerator = list.GetEnumerator();
        list.InsertRangeFromSpan(1, [10]);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.InsertRangeFromSpan(1, [10]));
    }

    [TestMethod]
    public void LastIndexOf()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.AreEqual(6, list.LastIndexOf(3));
        Assert.AreEqual(-1, list.LastIndexOf(7));
        Assert.AreEqual(6, list.LastIndexOf(3, 4));
        Assert.AreEqual(2, list.LastIndexOf(3, 2, 2));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 7));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, -1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 1, -1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 7, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 1, 7));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.LastIndexOf(1));
    }

    [TestMethod]
    public void Remove()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.IsTrue(list.Remove(3));
        CollectionAssert.AreEqual(new int[] { 1, 2, 4, 5, 6, 3 }, list);

        Assert.IsTrue(list.Remove(3));
        CollectionAssert.AreEqual(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.IsFalse(list.Remove(3));


        var enumerator = list.GetEnumerator();
        list.Remove(1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Remove(1));
    }

    [TestMethod]
    public void RemoveAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.AreEqual(2, list.RemoveAll(i => i == 3));
        CollectionAssert.AreEqual(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.AreEqual(0, list.RemoveAll(i => i == 9));


        var enumerator = list.GetEnumerator();
        list.RemoveAll(i => i == 1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.RemoveAll(i => i == 1));
    }

    [TestMethod]
    public void RemoveAt()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.RemoveAt(1);
        CollectionAssert.AreEqual(new int[] { 1, 3, 4, 5, 6 }, list);

        list.RemoveAt(1);
        CollectionAssert.AreEqual(new int[] { 1, 4, 5, 6 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.RemoveAt(7));


        var enumerator = list.GetEnumerator();
        list.RemoveAt(1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.RemoveAt(1));
    }

    [TestMethod]
    public void RemoveRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.RemoveRange(1, 3);
        CollectionAssert.AreEqual(new int[] { 1, 5, 6 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.RemoveRange(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.RemoveRange(1, -1));
        Assert.ThrowsException<ArgumentException>(() => list.RemoveRange(1, 7));
        Assert.ThrowsException<ArgumentException>(() => list.RemoveRange(7, 1));


        var enumerator = list.GetEnumerator();
        list.RemoveRange(1, 1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.RemoveRange(1, 1));
    }

    [TestMethod]
    public void Reverse()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.Reverse();
        CollectionAssert.AreEqual(new int[] { 6, 5, 4, 3, 2, 1 }, list);

        list.Reverse(1, 3);
        CollectionAssert.AreEqual(new int[] { 6, 3, 4, 5, 2, 1 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Reverse(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Reverse(1, -1));
        Assert.ThrowsException<ArgumentException>(() => list.Reverse(1, 7));
        Assert.ThrowsException<ArgumentException>(() => list.Reverse(7, 1));


        var enumerator = list.GetEnumerator();
        list.Reverse();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Reverse());
    }

    [TestMethod]
    public void Slice()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        using var ranged = list[1..4];
        CollectionAssert.AreEqual(new int[] { 2, 3, 4 }, ranged);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Slice(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Slice(1, -1));
        Assert.ThrowsException<ArgumentException>(() => list.Slice(1, 7));
        Assert.ThrowsException<ArgumentException>(() => list.Slice(7, 1));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Slice(1, 3));
    }

    [TestMethod]
    public void Sort()
    {
        var list = new ArrayPoolList<int>() { 2, 1, 5, 3, 4, 6 };

        list.Sort();
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 }, list);

        list.Sort((a, b) => b - a);
        CollectionAssert.AreEqual(new int[] { 6, 5, 4, 3, 2, 1 }, list);

        list.Sort(Comparer<int>.Default);
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 }, list);

        list.Sort(1, 3, Comparer<int>.Create((a, b) => b - a));
        CollectionAssert.AreEqual(new int[] { 1, 4, 3, 2, 5, 6 }, list);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Sort(-1, 1, Comparer<int>.Default));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => list.Sort(1, -1, Comparer<int>.Default));
        Assert.ThrowsException<ArgumentException>(() => list.Sort(1, 7, Comparer<int>.Default));
        Assert.ThrowsException<ArgumentException>(() => list.Sort(7, 1, Comparer<int>.Default));


        using var notSortableList = new ArrayPoolList<Random>() { new(), new() };
        Assert.ThrowsException<InvalidOperationException>(() => notSortableList.Sort());


        var enumerator = list.GetEnumerator();
        list.Sort();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.Sort());
    }

    [TestMethod]
    public void ToArray()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6 }, list.ToArray());


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.ToArray());
    }

    [TestMethod]
    public void TrimExcess()
    {
        var list = new ArrayPoolList<int>(256) { 1, 2, 3, 4, 5, 6 };

        Assert.AreEqual(256, list.Capacity);
        list.TrimExcess();
        Assert.AreEqual(16, list.Capacity);


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.TrimExcess());
    }

    [TestMethod]
    public void TrueForAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        Assert.IsTrue(list.TrueForAll(i => i < 10));
        Assert.IsFalse(list.TrueForAll(i => i > 3));

        using var emptyList = new ArrayPoolList<int>();
        Assert.IsTrue(list.TrueForAll(i => true));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => list.TrueForAll(i => true));
    }

    [TestMethod]
    public void Dispose()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        list.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(() => list[0]);

        // should not throw any exceptions
        list.Dispose();
    }

    [TestMethod]
    public void Add_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.AreEqual(6, ilist.Add(7));
        CollectionAssert.AreEqual(new int[] { 1, 2, 3, 4, 5, 6, 7 }, list);

        Assert.ThrowsException<ArgumentException>(() => ilist.Add("hoge"));
        Assert.ThrowsException<ArgumentException>(() => ilist.Add(null));


        var enumerator = list.GetEnumerator();
        ilist.Add(9);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist.Add(99));
    }

    [TestMethod]
    public void Contains_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.IsTrue(ilist.Contains(3));
        Assert.IsFalse(ilist.Contains(-1));
        Assert.IsFalse(ilist.Contains("hoge"));
        Assert.IsFalse(ilist.Contains(null));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist.Contains(99));
    }

    [TestMethod]
    public void IndexOf_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.AreEqual(2, ilist.IndexOf(3));
        Assert.AreEqual(-1, ilist.IndexOf(-1));
        Assert.AreEqual(-1, ilist.IndexOf("hoge"));
        Assert.AreEqual(-1, ilist.IndexOf(null));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist.IndexOf(99));
    }

    [TestMethod]
    public void Insert_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        ilist.Insert(1, 7);
        CollectionAssert.AreEqual(new int[] { 1, 7, 2, 3, 4, 5, 6 }, list);

        Assert.ThrowsException<ArgumentException>(() => ilist.Insert(1, "hoge"));
        Assert.ThrowsException<ArgumentException>(() => ilist.Insert(1, null));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ilist.Insert(-1, 1));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => ilist.Insert(99, 1));


        var enumerator = list.GetEnumerator();
        ilist.Insert(1, 9);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist.Insert(1, 9));
    }

    [TestMethod]
    public void Items_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.AreEqual(2, ilist[1]);
        ilist[1] = 7;
        Assert.AreEqual(7, ilist[1]);

        Assert.ThrowsException<ArgumentException>(() => ilist[1] = "hoge");
        Assert.ThrowsException<ArgumentException>(() => ilist[1] = null);
        Assert.ThrowsException<IndexOutOfRangeException>(() => ilist[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => ilist[99]);


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist[1]);
    }

    [TestMethod]
    public void Remove_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        ilist.Remove(3);
        CollectionAssert.AreEqual(new int[] { 1, 2, 4, 5, 6 }, list);

        ilist.Remove(7);
        CollectionAssert.AreEqual(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.ThrowsException<ArgumentException>(() => ilist.Remove("hoge"));
        Assert.ThrowsException<ArgumentException>(() => ilist.Remove(null));


        var enumerator = list.GetEnumerator();
        ilist.Remove(4);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ilist.Remove(4));
    }

    [TestMethod]
    public void CopyTo_ICollection()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var icollection = (ICollection)list;

        var target = new int[8];
        icollection.CopyTo(target, 1);
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, target);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => icollection.CopyTo(target, -1));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(target, 99));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(new int[8, 8], 1));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(new string[8], 1));


        list.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => icollection.CopyTo(target, 1));
    }
}
