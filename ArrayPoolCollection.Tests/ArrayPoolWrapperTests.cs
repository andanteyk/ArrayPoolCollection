using System.Collections;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolWrapperTests
{
    [Fact]
    public void Ctor()
    {
        // should not throw any exceptions
        using var one = new ArrayPoolWrapper<int>(1);
        using var zero = new ArrayPoolWrapper<int>(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolWrapper<int>(-1));
        Assert.Throws<OutOfMemoryException>(() => new ArrayPoolWrapper<int>(int.MaxValue));

        Assert.Equal(0, one[0]);
    }

    [Fact]
    public void Items()
    {
        var array = new ArrayPoolWrapper<int>(6);

        array[3] = 123;
        Assert.Equal(123, array[3]);

        Assert.Throws<IndexOutOfRangeException>(() => array[-1]);
        Assert.Throws<IndexOutOfRangeException>(() => array[123] = 1);

        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array[3]);
    }

    [Fact]
    public void Length()
    {
        var array = new ArrayPoolWrapper<int>(6);
        Assert.Equal(6, array.Length);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Length);
    }

    [Fact]
    public void LongLength()
    {
        var array = new ArrayPoolWrapper<int>(6);
        Assert.Equal(6L, array.LongLength);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.LongLength);
    }

    [Fact]
    public void AsReadOnly()
    {
        var array = new ArrayPoolWrapper<int>(6);
        array[3] = 123;

        var view = array.AsReadOnly();
        Assert.Equal(123, view[3]);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.AsReadOnly());
        Assert.Throws<ObjectDisposedException>(() => view[3]);
    }

    [Fact]
    public void AsSpan()
    {
        var array = new ArrayPoolWrapper<int>(6);
        for (int i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }


        Assert.Equal(6, array.AsSpan().Length);

        // implicit
        {
            Span<int> span = array;
            Assert.Equal(6, span.Length);
        }

        var fromOne = array.AsSpan(1);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5 }, fromOne.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(8));

        var fromLastTwo = array.AsSpan(^2);
        Assert.Equal(new int[] { 4, 5 }, fromLastTwo.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(^-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(^8));

        var centerTwo = array.AsSpan(2..4);
        Assert.Equal(new int[] { 2, 3 }, centerTwo.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(-1..3));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(1..-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(8..1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(1..8));

        var centerFour = array.AsSpan(1, 4);
        Assert.Equal(new int[] { 1, 2, 3, 4 }, centerFour.ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(-1, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(1, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(8, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.AsSpan(1, 8));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.AsSpan());
    }

    [Fact]
    public void BinarySearch()
    {
        var array = Enumerable.Range(0, 64).Select(i => i * 2).ToArrayPool();

        Assert.Equal(4, array.BinarySearch(8));
        Assert.Equal(~4, array.BinarySearch(7));

        Assert.Equal(~10, array.BinarySearch(10, 10, 8));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.BinarySearch(8));
    }

    [Fact]
    public void Clear()
    {
        var array = new ArrayPoolWrapper<int>(8, false);
        array.Clear();
        Assert.Equal(new int[8], array);


        for (int i = 0; i < array.Length; i++)
        {
            array[i] = i;
        }

        array.Clear(1, 3);
        Assert.Equal(new int[] { 0, 0, 0, 0, 4, 5, 6, 7 }, array);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Clear());
    }

    [Fact]
    public void Clone()
    {
        var source = Enumerable.Range(0, 100).ToArrayPool();
        using var clone = source.Clone();

        Assert.Equal(clone, source);


        source.Dispose();
        Assert.Throws<ObjectDisposedException>(() => source.Clone());
    }

    [Fact]
    public void ConvertAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        using var doubleArray = array.ConvertAll(i => i * 2.0);

        Assert.Equal(Enumerable.Range(0, 100).Select(i => i * 2.0).ToArray(), doubleArray);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.ConvertAll(i => i * 2.0));
    }

    [Fact]
    public void CopyTo()
    {
        var array = Enumerable.Range(1, 6).ToArrayPool();

        var dest = new int[10];
        array.CopyTo(dest, 2);
        Assert.Equal(new int[] { 0, 0, 1, 2, 3, 4, 5, 6, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest);
        Assert.Equal(new int[] { 1, 2, 3, 4, 5, 6, 0, 0, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest.AsSpan(1..));
        Assert.Equal(new int[] { 0, 1, 2, 3, 4, 5, 6, 0, 0, 0 }, dest);

        Array.Clear(dest, 0, dest.Length);
        array.CopyTo(dest.AsMemory(3..));
        Assert.Equal(new int[] { 0, 0, 0, 1, 2, 3, 4, 5, 6, 0 }, dest);


        Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyTo(dest, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.CopyTo(dest, 11));
        Assert.Throws<ArgumentException>(() => array.CopyTo(dest, 8));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.CopyTo(dest, 0));
    }

    [Fact]
    public void Dispose()
    {
        var array = new ArrayPoolWrapper<int>(6);
        array.Dispose();

        // should not throw any exceptions
        array.Dispose();
    }

    [Fact]
    public void Exists()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.True(array.Exists(i => i == 23));
        Assert.False(array.Exists(i => i == -1));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Exists(i => i == 1));
    }

    [Fact]
    public void Fill()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.Fill(123);
        Assert.Equal(Enumerable.Repeat(123, 100).ToArray(), array);


        array.Fill(456, 25, 50);
        Assert.Equal(Enumerable.Range(0, 100).Select(i => 25 <= i && i < 75 ? 456 : 123).ToArray(), array);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Fill(123));
    }

    [Fact]
    public void Find()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        Assert.Equal(12, array.Find(i => i == 12));
        Assert.Equal(default, array.Find(i => i < 0));

        using var stringArray = Enumerable.Repeat("hoge", 100).ToArrayPool();
        Assert.Null(stringArray.Find(s => s == "fuga"));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Find(i => i == 10));
    }

    [Fact]
    public void FindAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        using var inRange = array.FindAll(i => 25 <= i && i < 75);
        Assert.Equal(Enumerable.Range(25, 50).ToArray(), inRange);

        using var empty = array.FindAll(i => i < 0);
        Assert.Equal(new int[0], empty);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.FindAll(i => i == 1));
    }

    [Fact]
    public void FindIndex()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(11, array.FindIndex(i => i % 12 == 11));
        Assert.Equal(23, array.FindIndex(20, i => i % 12 == 11));
        Assert.Equal(-1, array.FindIndex(12, 10, i => i % 12 == 11));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.FindIndex(i => i == 1));
    }

    [Fact]
    public void FindLast()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(95, array.FindLast(i => i % 12 == 11));
        Assert.Equal(default, array.FindLast(i => i < 0));

        var stringArray = Enumerable.Repeat("hoge", 100).ToArrayPool();
        Assert.Null(stringArray.FindLast(s => s == "fuga"));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.FindLast(i => i == 0));
    }

    [Fact]
    public void FindLastIndex()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(95, array.FindLastIndex(i => i % 12 == 11));
        Assert.Equal(95, array.FindLastIndex(20, i => i % 12 == 11));
        Assert.Equal(-1, array.FindLastIndex(12, 10, i => i % 12 == 11));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.FindLastIndex(i => i == 1));
    }

    [Fact]
    public void ForEach()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.ForEach(i => Assert.True(i >= 0));
        array.ForEach((i, index) => Assert.Equal(index, i));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.ForEach(i => Assert.Fail()));
    }

    [Fact]
    public void GetEnumerator()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        int expected = 0;
        foreach (var value in array)
        {
            Assert.Equal(expected, value);
            expected++;
        }


        var enumerator = array.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.True(enumerator.MoveNext());


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void IndexOf()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(12, array.IndexOf(12));
        Assert.Equal(-1, array.IndexOf(123));

        Assert.Equal(-1, array.IndexOf(12, 20));
        Assert.Equal(-1, array.IndexOf(48, 1, 16));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.IndexOf(12));
    }

    [Fact]
    public void LastIndexOf()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(12, array.LastIndexOf(12));
        Assert.Equal(-1, array.LastIndexOf(123));

        Assert.Equal(-1, array.LastIndexOf(12, 20));
        Assert.Equal(-1, array.LastIndexOf(48, 1, 16));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.LastIndexOf(12));
    }

    [Fact]
    public void Resize()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        ArrayPoolWrapper<int>.Resize(ref array, 200);
        Assert.Equal(Enumerable.Range(0, 100).Concat(Enumerable.Repeat(0, 100)).ToArray(), array);

        ArrayPoolWrapper<int>.Resize(ref array, 50);
        Assert.Equal(Enumerable.Range(0, 50).ToArray(), array);

        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolWrapper<int>.Resize(ref array, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => ArrayPoolWrapper<int>.Resize(ref array, int.MaxValue));


        var enumerator = array.GetEnumerator();
        ArrayPoolWrapper<int>.Resize(ref array, 100);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolWrapper<int>.Resize(ref array, 123));
    }

    [Fact]
    public void Reverse()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        array.Reverse();
        Assert.Equal(Enumerable.Range(0, 100).Reverse().ToArray(), array);

        array.Reverse();
        array.Reverse(12, 34);

        Assert.Equal(
            Enumerable.Range(0, 12).Concat(Enumerable.Range(12, 34).Reverse()).Concat(Enumerable.Range(46, 54)).ToArray(),
            array);

        Assert.Throws<ArgumentOutOfRangeException>(() => array.Reverse(-1, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Reverse(12, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Reverse(12, 123));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Reverse());
    }

    [Fact]
    public void Slice()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.Equal(Enumerable.Range(12, 34).ToArray(), array.Slice(12, 34).ToArray());
        Assert.Equal(Enumerable.Range(56, 22).ToArray(), array[56..78].ToArray());

        Assert.Throws<ArgumentOutOfRangeException>(() => array.Slice(-1, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Slice(12, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Slice(12, 123));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Slice(12, 34));
    }

    [Fact]
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
        Assert.Equal(Enumerable.Range(0, 100).ToArray(), array);

        Shuffle(array.AsSpan(), rng);
        array.Sort((a, b) => b - a);
        Assert.Equal(Enumerable.Range(0, 100).Reverse().ToArray(), array);

        array.Sort(25, 50);
        Assert.Equal(Enumerable.Range(75, 25).Reverse().Concat(Enumerable.Range(25, 50)).Concat(Enumerable.Range(0, 25).Reverse()).ToArray(), array);


        Assert.Throws<ArgumentOutOfRangeException>(() => array.Sort(-1, 12));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Sort(12, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => array.Sort(12, 123));


        using ArrayPoolWrapper<string> stringArray = ["Alice", "abigail", "Barbara", "Charlotte"];
        Shuffle(stringArray.AsSpan(), rng);
        stringArray.Sort(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(new string[] { "abigail", "Alice", "Barbara", "Charlotte" }, stringArray);


        array.Sort();
        var keyArray = Enumerable.Range(0, 100).Reverse().ToArrayPool();
        ArrayPoolWrapper<int>.Sort(keyArray, array);
        Assert.Equal(Enumerable.Range(0, 100).Reverse().ToArray(), array);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.Sort());
    }

    [Fact]
    public void TrueForAll()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();

        Assert.True(array.TrueForAll(i => i >= 0));
        Assert.False(array.TrueForAll(i => i == 12));


        using var empty = new ArrayPoolWrapper<int>(0);
        Assert.True(empty.TrueForAll(i => false));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => array.TrueForAll(i => true));
    }

    [Fact]
    public void Create()
    {
        using ArrayPoolWrapper<int> array = [2, 3, 5, 7, 11, 13, 17, 19];
        Assert.Equal(new int[] { 2, 3, 5, 7, 11, 13, 17, 19 }, array);
    }

    [Fact]
    public void Contains_ICollectionT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection<int> collection = array;

        Assert.True(collection.Contains(12));
        Assert.False(collection.Contains(-1));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => collection.Contains(12));
    }

    [Fact]
    public void CopyTo_ICollectionT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection<int> collection = array;

        var buffer = new int[128];
        collection.CopyTo(buffer, 10);
        Assert.Equal(Enumerable.Repeat(0, 10).Concat(Enumerable.Range(0, 100)).Concat(Enumerable.Repeat(0, 18)).ToArray(), buffer);


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => collection.CopyTo(buffer, 0));
    }

    [Fact]
    public void IndexOf_IListT()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList<int> list = array;

        Assert.Equal(12, list.IndexOf(12));
        Assert.Equal(-1, list.IndexOf(-1));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.IndexOf(12));
    }

    [Fact]
    public void Contains_IList()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList list = array;

        Assert.True(list.Contains(12));
        Assert.False(list.Contains(-1));
        Assert.False(list.Contains("hoge"));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.Contains(12));

    }

    [Fact]
    public void IndexOf_IList()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        IList list = array;

        Assert.Equal(12, list.IndexOf(12));
        Assert.Equal(-1, list.IndexOf(-1));
        Assert.Equal(-1, list.IndexOf("hoge"));


        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => list.IndexOf(12));

    }

    [Fact]
    public void CopyTo_ICollection()
    {
        var array = Enumerable.Range(0, 100).ToArrayPool();
        ICollection collection = array;

        var buffer = new int[128];
        collection.CopyTo(buffer, 10);
        Assert.Equal(Enumerable.Repeat(0, 10).Concat(Enumerable.Range(0, 100)).Concat(Enumerable.Repeat(0, 18)).ToArray(), buffer);

        var stringBuffer = new string[128];
        Assert.Throws<ArgumentException>(() => collection.CopyTo(stringBuffer, 0));

        array.Dispose();
        Assert.Throws<ObjectDisposedException>(() => collection.CopyTo(buffer, 0));

    }
}
