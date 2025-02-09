using System.Collections;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolListTests
{
    [Fact]
    public void Ctor()
    {
        // should not throw any exceptions
        using var defaultList = new ArrayPoolList<int>();

        // should not throw any exceptions
        using var emptyList = new ArrayPoolList<int>(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolList<int>(-1));

        using var enumerableList = new ArrayPoolList<int>(Enumerable.Range(0, 24));
        Assert.Equal(Enumerable.Range(0, 24).ToList(), enumerableList);
    }

    [Fact]
    public void Capacity()
    {
        var list = new ArrayPoolList<int>(32);
        Assert.Equal(32, list.Capacity);

        list.Capacity = 48;
        Assert.Equal(64, list.Capacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Capacity = -1);

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Capacity);
    }

    [Fact]
    public void Items()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.Equal(1, list[0]);
        list[0] = 123;
        Assert.Equal(123, list[0]);

        Assert.Throws<IndexOutOfRangeException>(() => list[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => list[4] = -4);

        var ilist = (IList)list;
        Assert.Equal(123, ilist[0]);
        ilist[0] = 456;
        Assert.Equal(456, ilist[0]);
        Assert.Throws<ArgumentException>(() => ilist[0] = "hoge");

        Assert.Throws<IndexOutOfRangeException>(() => ilist[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => ilist[4] = -4);

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        var ilistString = (IList)stringList;
        ilistString[0] = null;
        Assert.Null(ilistString[0]);
        Assert.Throws<ArgumentException>(() => ilistString[0] = 123);

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list[0]);
        Assert.Throws<ObjectDisposedException>(() => list[0] = -1);
    }

    [Fact]
    public void Count()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.Equal(3, list.Count);
        list.Add(4);
        Assert.Equal(4, list.Count);

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Count);
    }

    [Fact]
    public void Add()
    {
        var list = new ArrayPoolList<int>();
        list.Add(1);

        Assert.Equal(1, list[0]);
        Assert.Single(list);

        list.Add(2);
        Assert.Equal(2, list[1]);
        Assert.Equal(2, list.Count);

        for (int i = 0; i < 98; i++)
        {
            list.Add(i);
        }
        Assert.Equal(100, list.Count);


        var enumerator = list.GetEnumerator();
        list.Add(-1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Add(-2));
    }

    [Fact]
    public void AddRange()
    {
        var list = new ArrayPoolList<int>();

        // TryGetNonEnumeratedCount == true, but not ICollection<T>
        list.AddRange(Enumerable.Range(0, 16));
        Assert.Equal(Enumerable.Range(0, 16).ToArray(), list);

        // TryGetNonEnumeratedCount == true, ICollection<T>
        list.AddRange(Enumerable.Range(16, 16).ToArray());
        Assert.Equal(Enumerable.Range(0, 32).ToArray(), list);

        static IEnumerable<int> Iter()
        {
            for (int i = 32; i < 48; i++)
            {
                yield return i;
            }
        }

        // TryGetNonEnumeratedCount == false
        list.AddRange(Iter());
        Assert.Equal(Enumerable.Range(0, 48).ToArray(), list);

        list.AddRange([]);
        Assert.Equal(Enumerable.Range(0, 48).ToArray(), list);

        list.AddRange(Enumerable.Range(48, 16).ToArray().AsSpan());
        Assert.Equal(Enumerable.Range(0, 64).ToArray(), list);


        var enumerator = list.GetEnumerator();
        list.AddRange(Enumerable.Range(0, 16));
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        enumerator = list.GetEnumerator();
        list.AddRange(Enumerable.Range(0, 16).ToArray().AsSpan());
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.AddRange(Enumerable.Range(0, 16)));
        Assert.Throws<ObjectDisposedException>(() => list.AddRange(Enumerable.Range(0, 16).ToArray().AsSpan()));
    }

    [Fact]
    public void AsReadOnly()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        var readonlyList = list.AsReadOnly();

        Assert.Equal(new int[] { 1, 2, 3 }, readonlyList);

        list.Add(4);
        Assert.Equal(new int[] { 1, 2, 3, 4 }, readonlyList);


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.AsReadOnly());
        Assert.Throws<ObjectDisposedException>(() => readonlyList[0]);
    }

    [Fact]
    public void AsSpan()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        var span = ArrayPoolList<int>.AsSpan(list);

        Assert.Equal(new int[] { 1, 2, 3 }, span.ToArray());


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolList<int>.AsSpan(list));
    }

    [Fact]
    public void BinarySearch()
    {
        var list = new ArrayPoolList<int>() { 2, 4, 6, 8, 10 };

        Assert.Equal(0, list.BinarySearch(2));
        Assert.Equal(2, list.BinarySearch(6));
        Assert.Equal(~0, list.BinarySearch(1));
        Assert.Equal(~3, list.BinarySearch(7));
        Assert.Equal(~5, list.BinarySearch(11));

        Assert.Equal(1, list.BinarySearch(4, Comparer<int>.Create((a, b) => a - b)));
        Assert.Throws<ArgumentNullException>(() => list.BinarySearch(8, null!));

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.BinarySearch(2));
    }

    [Fact]
    public void Clear()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        list.Clear();
        Assert.Empty(list);

        list.Clear();
        Assert.Empty(list);


        var enumerator = list.GetEnumerator();
        list.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Clear());
    }

    [Fact]
    public void Contains()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        Assert.True(list.Contains(1));
        Assert.False(list.Contains(-1));

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.True(stringList.Contains("hoge"));
        Assert.False(stringList.Contains(null!));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Contains(1));
    }

    [Fact]
    public void ConvertAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };

        using var doubleList = list.ConvertAll(i => (double)i);
        Assert.Equal(new double[] { 1.0, 2.0, 3.0 }, doubleList);


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.ConvertAll(i => (double)i));
    }

    [Fact]
    public void CopyTo()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var buffer = new int[8];

        list.CopyTo(2, buffer, 3, 4);
        Assert.Equal(new int[] { 0, 0, 0, 3, 4, 5, 6, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6, 0, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer, 1);
        Assert.Equal(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, buffer);

        buffer.AsSpan().Clear();
        list.CopyTo(buffer.AsSpan(1..7));
        Assert.Equal(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, buffer);


        Assert.Throws<ArgumentNullException>(() => list.CopyTo(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(-1, buffer, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(0, buffer, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.CopyTo(0, buffer, 0, -1));
        Assert.Throws<ArgumentException>(() => list.CopyTo(10, buffer, 0, 1));
        Assert.Throws<ArgumentException>(() => list.CopyTo(1, buffer, 7, 8));
        Assert.Throws<ArgumentException>(() => list.CopyTo(buffer.AsSpan(1..2)));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.CopyTo(buffer));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var list = new ArrayPoolList<int>();
        Assert.Equal(64, list.EnsureCapacity(48));
        Assert.Equal(64, list.Capacity);

        Assert.Equal(64, list.EnsureCapacity(16));
        Assert.Equal(64, list.Capacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.EnsureCapacity(-1));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.EnsureCapacity(64));
    }

    [Fact]
    public void Exists()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        Assert.True(list.Exists(i => i == 2));
        Assert.True(list.Exists(i => i < 4));
        Assert.False(list.Exists(i => i == 4));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Exists(i => i == 2));
    }

    [Fact]
    public void Find()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3 };
        Assert.Equal(2, list.Find(i => i == 2));
        Assert.Equal(1, list.Find(i => i < 4));
        Assert.Equal(0, list.Find(i => i == 4));

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.Equal("hoge", stringList.Find(s => s.Length < 5));
        Assert.Null(stringList.Find(s => s.Length >= 5));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Find(i => i == 2));
    }

    [Fact]
    public void FindAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(new int[] { 2, 4, 6 }, list.FindAll(i => i % 2 == 0));
        Assert.Equal(new int[0], list.FindAll(i => i > 10));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.FindAll(i => i == 2));
    }

    [Fact]
    public void FindIndex()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(2, list.FindIndex(i => i == 3));
        Assert.Equal(1, list.FindIndex(i => i % 2 == 0));
        Assert.Equal(-1, list.FindIndex(i => i == 10));

        Assert.Equal(2, list.FindIndex(1, i => i % 2 == 1));
        Assert.Equal(2, list.FindIndex(1, 3, i => i % 2 == 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, i => i == 3));
        Assert.Throws<ArgumentException>(() => list.FindIndex(10, i => i == 3));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(-1, 1, i => i == 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindIndex(1, -1, i => i == 3));
        Assert.Throws<ArgumentException>(() => list.FindIndex(1, 10, i => i == 3));
        Assert.Throws<ArgumentException>(() => list.FindIndex(10, 1, i => i == 3));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.FindIndex(i => i == 3));
    }

    [Fact]
    public void FindLast()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(5, list.FindLast(i => i % 2 == 1));
        Assert.Equal(0, list.FindLast(i => i > 10));

        using var stringList = new ArrayPoolList<string>() { "hoge", "fuga", "piyo" };
        Assert.Equal("piyo", stringList.FindLast(s => s.Length < 5));
        Assert.Null(stringList.FindLast(s => s.Length >= 5));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.FindLast(i => i == 3));
    }


    [Fact]
    public void FindLastIndex()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(2, list.FindLastIndex(i => i == 3));
        Assert.Equal(5, list.FindLastIndex(i => i % 2 == 0));
        Assert.Equal(-1, list.FindLastIndex(i => i == 10));

        Assert.Equal(4, list.FindLastIndex(1, i => i % 2 == 1));
        Assert.Equal(2, list.FindLastIndex(1, 3, i => i % 2 == 1));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, i => i == 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(10, i => i == 3));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(-1, 1, i => i == 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(1, -1, i => i == 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(1, 10, i => i == 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.FindLastIndex(10, 1, i => i == 3));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.FindLastIndex(i => i == 3));
    }

    [Fact]
    public void ForEach()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        using var list2 = new ArrayPoolList<double>();

        list.ForEach(i => list2.Add(i * 2.0));
        Assert.Equal(new double[] { 2.0, 4.0, 6.0, 8.0, 10.0, 12.0 }, list2);

        Assert.Throws<InvalidOperationException>(() => list.ForEach(i => list.Add(i * 2)));
        Assert.Throws<InvalidOperationException>(() => list.ForEach(i => list.Remove(i)));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.ForEach(i => list2.Add(i * 2.0)));
    }

    [Fact]
    public void GetEnumerator()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        {
            using var enumerator = list.GetEnumerator();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);

            for (int i = 0; enumerator.MoveNext(); i++)
            {
                Assert.Equal(i + 1, enumerator.Current);
            }

            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            enumerator.MoveNext();
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        {
            using var enumerator = list.GetEnumerator();
            enumerator.MoveNext();
            list.Add(7);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }

        {
            using var enumerator = list.GetEnumerator();
            enumerator.MoveNext();
            list.Remove(7);
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        }


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.GetEnumerator());
    }

    [Fact]
    public void GetRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        using var ranged = list.GetRange(1, 3);
        Assert.Equal(new int[] { 2, 3, 4 }, ranged);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.GetRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.GetRange(1, -1));
        Assert.Throws<ArgumentException>(() => list.GetRange(1, 7));
        Assert.Throws<ArgumentException>(() => list.GetRange(7, 1));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.GetRange(1, 3));
    }

    [Fact]
    public void IndexOf()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        Assert.Equal(2, list.IndexOf(3));
        Assert.Equal(-1, list.IndexOf(7));
        Assert.Equal(-1, list.IndexOf(3, 4));
        Assert.Equal(2, list.IndexOf(3, 2, 4));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, 7, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.IndexOf(3, 1, 7));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.IndexOf(1));
    }

    [Fact]
    public void Insert()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.Insert(1, 9);
        Assert.Equal(new int[] { 1, 9, 2, 3, 4, 5, 6 }, list);

        list.Insert(7, 8);
        Assert.Equal(new int[] { 1, 9, 2, 3, 4, 5, 6, 8 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(-1, 99));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Insert(10, 99));


        var enumerator = list.GetEnumerator();
        list.Insert(1, 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Insert(1, 10));
    }

    [Fact]
    public void InsertRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        // TryNonEnumeratedCount == true, but not ICollection<T>
        list.InsertRange(1, Enumerable.Range(10, 3));
        Assert.Equal(new int[] { 1, 10, 11, 12, 2, 3, 4, 5, 6 }, list);

        // TryNonEnumeratedCount == true, and also ICollection<T>
        list.InsertRange(2, Enumerable.Range(20, 3).ToArray());
        Assert.Equal(new int[] { 1, 10, 20, 21, 22, 11, 12, 2, 3, 4, 5, 6 }, list);

        static IEnumerable<int> Ints()
        {
            yield return 30;
            yield return 31;
            yield return 32;
        }

        // TryNonEnumeratedCount == false
        list.InsertRange(3, Ints());
        Assert.Equal(new int[] { 1, 10, 20, 30, 31, 32, 21, 22, 11, 12, 2, 3, 4, 5, 6 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, [99]));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(99, [99]));


        var enumerator = list.GetEnumerator();
        list.InsertRange(1, [10]);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.InsertRange(1, [10]));
    }

    [Fact]
    public void InsertRangeFromSpan()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.InsertRange(1, Enumerable.Range(10, 3).ToArray());
        Assert.Equal(new int[] { 1, 10, 11, 12, 2, 3, 4, 5, 6 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(-1, [99]));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.InsertRange(99, [99]));


        var enumerator = list.GetEnumerator();
        list.InsertRange(1, [10]);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.InsertRange(1, [10]));
    }

    [Fact]
    public void LastIndexOf()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.Equal(6, list.LastIndexOf(3));
        Assert.Equal(-1, list.LastIndexOf(7));
        Assert.Equal(6, list.LastIndexOf(3, 4));
        Assert.Equal(2, list.LastIndexOf(3, 2, 2));

        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 7));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, -1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 7, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.LastIndexOf(3, 1, 7));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.LastIndexOf(1));
    }

    [Fact]
    public void Remove()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.True(list.Remove(3));
        Assert.Equal(new int[] { 1, 2, 4, 5, 6, 3 }, list);

        Assert.True(list.Remove(3));
        Assert.Equal(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.False(list.Remove(3));


        var enumerator = list.GetEnumerator();
        list.Remove(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Remove(1));
    }

    [Fact]
    public void RemoveAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6, 3 };

        Assert.Equal(2, list.RemoveAll(i => i == 3));
        Assert.Equal(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.Equal(0, list.RemoveAll(i => i == 9));


        var enumerator = list.GetEnumerator();
        list.RemoveAll(i => i == 1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.RemoveAll(i => i == 1));
    }

    [Fact]
    public void RemoveAt()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.RemoveAt(1);
        Assert.Equal(new int[] { 1, 3, 4, 5, 6 }, list);

        list.RemoveAt(1);
        Assert.Equal(new int[] { 1, 4, 5, 6 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveAt(7));


        var enumerator = list.GetEnumerator();
        list.RemoveAt(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.RemoveAt(1));
    }

    [Fact]
    public void RemoveRange()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.RemoveRange(1, 3);
        Assert.Equal(new int[] { 1, 5, 6 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveRange(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.RemoveRange(1, -1));
        Assert.Throws<ArgumentException>(() => list.RemoveRange(1, 7));
        Assert.Throws<ArgumentException>(() => list.RemoveRange(7, 1));


        var enumerator = list.GetEnumerator();
        list.RemoveRange(1, 1);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.RemoveRange(1, 1));
    }

    [Fact]
    public void Reverse()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        list.Reverse();
        Assert.Equal(new int[] { 6, 5, 4, 3, 2, 1 }, list);

        list.Reverse(1, 3);
        Assert.Equal(new int[] { 6, 3, 4, 5, 2, 1 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.Reverse(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Reverse(1, -1));
        Assert.Throws<ArgumentException>(() => list.Reverse(1, 7));
        Assert.Throws<ArgumentException>(() => list.Reverse(7, 1));


        var enumerator = list.GetEnumerator();
        list.Reverse();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Reverse());
    }

    [Fact]
    public void SetCount()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        ArrayPoolList<int>.SetCount(list, 12);
        Assert.Equal(12, list.Count);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolList<int>.SetCount(list, -1));
        Assert.Throws<ArgumentException>(() => ArrayPoolList<int>.SetCount(list, 99));


        var enumerator = list.GetEnumerator();
        ArrayPoolList<int>.SetCount(list, 13);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolList<int>.SetCount(list, 1));
    }

    [Fact]
    public void Slice()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        using var ranged = list[1..4];
        Assert.Equal(new int[] { 2, 3, 4 }, ranged);

        Assert.Throws<ArgumentOutOfRangeException>(() => list.Slice(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Slice(1, -1));
        Assert.Throws<ArgumentException>(() => list.Slice(1, 7));
        Assert.Throws<ArgumentException>(() => list.Slice(7, 1));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Slice(1, 3));
    }

    [Fact]
    public void Sort()
    {
        var list = new ArrayPoolList<int>() { 2, 1, 5, 3, 4, 6 };

        list.Sort();
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6 }, list);

        list.Sort((a, b) => b - a);
        Assert.Equal(new int[] { 6, 5, 4, 3, 2, 1 }, list);

        list.Sort(Comparer<int>.Default);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6 }, list);

        list.Sort(1, 3, Comparer<int>.Create((a, b) => b - a));
        Assert.Equal(new int[] { 1, 4, 3, 2, 5, 6 }, list);


        Assert.Throws<ArgumentOutOfRangeException>(() => list.Sort(-1, 1, Comparer<int>.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => list.Sort(1, -1, Comparer<int>.Default));
        Assert.Throws<ArgumentException>(() => list.Sort(1, 7, Comparer<int>.Default));
        Assert.Throws<ArgumentException>(() => list.Sort(7, 1, Comparer<int>.Default));


        using var notSortableList = new ArrayPoolList<Random>() { new(), new() };
        Assert.Throws<InvalidOperationException>(() => notSortableList.Sort());


        var enumerator = list.GetEnumerator();
        list.Sort();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Sort());
    }

    [Fact]
    public void ToArray()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6 }, list.ToArray());


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.ToArray());
    }

    [Fact]
    public void TrimExcess()
    {
        var list = new ArrayPoolList<int>(256) { 1, 2, 3, 4, 5, 6 };

        Assert.Equal(256, list.Capacity);
        list.TrimExcess();
        Assert.Equal(16, list.Capacity);


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.TrimExcess());
    }

    [Fact]
    public void TrueForAll()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };

        Assert.True(list.TrueForAll(i => i < 10));
        Assert.False(list.TrueForAll(i => i > 3));

        using var emptyList = new ArrayPoolList<int>();
        Assert.True(list.TrueForAll(i => true));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.TrueForAll(i => true));
    }

    [Fact]
    public void Dispose()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        list.Dispose();

        Assert.Throws<ObjectDisposedException>(() => list[0]);

        // should not throw any exceptions
        list.Dispose();
    }

    [Fact]
    public void Add_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.Equal(6, ilist.Add(7));
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6, 7 }, list);

        Assert.Throws<ArgumentException>(() => ilist.Add("hoge"));
        Assert.Throws<ArgumentException>(() => ilist.Add(null));


        var enumerator = list.GetEnumerator();
        ilist.Add(9);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist.Add(99));
    }

    [Fact]
    public void Contains_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.True(ilist.Contains(3));
        Assert.False(ilist.Contains(-1));
        Assert.False(ilist.Contains("hoge"));
        Assert.False(ilist.Contains(null));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist.Contains(99));
    }

    [Fact]
    public void IndexOf_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.Equal(2, ilist.IndexOf(3));
        Assert.Equal(-1, ilist.IndexOf(-1));
        Assert.Equal(-1, ilist.IndexOf("hoge"));
        Assert.Equal(-1, ilist.IndexOf(null));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist.IndexOf(99));
    }

    [Fact]
    public void Insert_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        ilist.Insert(1, 7);
        Assert.Equal(new int[] { 1, 7, 2, 3, 4, 5, 6 }, list);

        Assert.Throws<ArgumentException>(() => ilist.Insert(1, "hoge"));
        Assert.Throws<ArgumentException>(() => ilist.Insert(1, null));
        Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ilist.Insert(99, 1));


        var enumerator = list.GetEnumerator();
        ilist.Insert(1, 9);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist.Insert(1, 9));
    }

    [Fact]
    public void Items_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        Assert.Equal(2, ilist[1]);
        ilist[1] = 7;
        Assert.Equal(7, ilist[1]);

        Assert.Throws<ArgumentException>(() => ilist[1] = "hoge");
        Assert.Throws<ArgumentException>(() => ilist[1] = null);
        Assert.Throws<IndexOutOfRangeException>(() => ilist[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => ilist[99]);


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist[1]);
    }

    [Fact]
    public void Remove_IList()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var ilist = (IList)list;

        ilist.Remove(3);
        Assert.Equal(new int[] { 1, 2, 4, 5, 6 }, list);

        ilist.Remove(7);
        Assert.Equal(new int[] { 1, 2, 4, 5, 6 }, list);

        Assert.Throws<ArgumentException>(() => ilist.Remove("hoge"));
        Assert.Throws<ArgumentException>(() => ilist.Remove(null));


        var enumerator = list.GetEnumerator();
        ilist.Remove(4);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ilist.Remove(4));
    }

    [Fact]
    public void CopyTo_ICollection()
    {
        var list = new ArrayPoolList<int>() { 1, 2, 3, 4, 5, 6 };
        var icollection = (ICollection)list;

        var target = new int[8];
        icollection.CopyTo(target, 1);
        Assert.Equal(new int[] { 0, 1, 2, 3, 4, 5, 6, 0 }, target);

        Assert.Throws<ArgumentOutOfRangeException>(() => icollection.CopyTo(target, -1));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(target, 99));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(new int[8, 8], 1));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(new string[8], 1));


        list.Dispose();
        Assert.Throws<ObjectDisposedException>(() => icollection.CopyTo(target, 1));
    }
}
