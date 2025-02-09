using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolDictionaryTests
{
    [Fact]
    public void Ctor()
    {
        var source = new Dictionary<int, int>(){
            {1, 2},
            {2, 4},
            {3, 6},
        };
        var comparer = EqualityComparer<int>.Default;

        // should not throw any exceptions
        {
            using var noarg = new ArrayPoolDictionary<int, int>();
        }

        // should not throw any exceptions
        {
            using var withComparer = new ArrayPoolDictionary<int, int>(comparer);
            using var withNullComparer = new ArrayPoolDictionary<int, int>(comparer: null);
        }

        {
            // should not throw any exceptions
            using var withCapacity = new ArrayPoolDictionary<int, int>(0);
            Assert.Equal(16, withCapacity.Capacity);

            Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolDictionary<int, int>(-1));
            Assert.Throws<OutOfMemoryException>(() => new ArrayPoolDictionary<int, int>(int.MaxValue));
        }

        // should not throw any exceptions
        {
            using var withCapacityAndComparer = new ArrayPoolDictionary<int, int>(0, comparer);
            using var withCapacityAndNullComparer = new ArrayPoolDictionary<int, int>(0, null);
        }

        {
            using var withSource = new ArrayPoolDictionary<int, int>(source);
            Assert.Equivalent(source, withSource);

            using var withSourceAndComparer = new ArrayPoolDictionary<int, int>(source, comparer);
            Assert.Equivalent(source, withSource);
        }

        {
            using var withSource = new ArrayPoolDictionary<int, int>(source.AsEnumerable());
            Assert.Equivalent(source, withSource);

            using var withSourceAndComparer = new ArrayPoolDictionary<int, int>(source.AsEnumerable(), comparer);
            Assert.Equivalent(source, withSource);
        }
    }

    [Fact]
    public void Comparer()
    {
        var valueComparer = EqualityComparer<int>.Default;
        var classComparer = StringComparer.OrdinalIgnoreCase;

        var valueWithNull = new ArrayPoolDictionary<int, int>();
        Assert.Equal(valueComparer, valueWithNull.Comparer);

        var valueWithComparer = new ArrayPoolDictionary<int, int>(valueComparer);
        Assert.Equal(valueComparer, valueWithComparer.Comparer);

        var classWithNull = new ArrayPoolDictionary<string, string>();
        Assert.Equal(EqualityComparer<string>.Default, classWithNull.Comparer);

        var classWithComparer = new ArrayPoolDictionary<string, string>(classComparer);
        Assert.Equal(classComparer, classWithComparer.Comparer);


        valueWithNull.Dispose();
        valueWithComparer.Dispose();
        classWithNull.Dispose();
        classWithComparer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => valueWithNull.Comparer);
        Assert.Throws<ObjectDisposedException>(() => valueWithComparer.Comparer);
        Assert.Throws<ObjectDisposedException>(() => classWithNull.Comparer);
        Assert.Throws<ObjectDisposedException>(() => classWithComparer.Comparer);
    }

    [Fact]
    public void Capacity()
    {
        var dict = new ArrayPoolDictionary<int, int>(48);
        Assert.Equal(64, dict.Capacity);

        dict.EnsureCapacity(192);
        Assert.Equal(256, dict.Capacity);


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Capacity);
    }

    [Fact]
    public void Count()
    {
        var dict = new ArrayPoolDictionary<int, int>(32);
        Assert.Empty(dict);

        dict.Add(1, 2);
        Assert.Single(dict);

        dict.Add(2, 4);
        Assert.Equal(2, dict.Count);

        dict.Remove(1);
        Assert.Single(dict);


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Count);
    }

    [Fact]
    public void Items()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        Assert.Equal(2, dict[1]);
        Assert.Equal(4, dict[2]);
        Assert.Equal(6, dict[3]);
        Assert.Throws<KeyNotFoundException>(() => dict[4]);

        dict[4] = 8;
        Assert.Equal(8, dict[4]);

        dict[1] = 123;
        Assert.Equal(123, dict[1]);

        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        dict[1] = 1234;
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        using var stringDict = new ArrayPoolDictionary<string, string>() { { "hoge", "fuga" } };
        Assert.Equal("fuga", stringDict["hoge"]);
        Assert.Throws<ArgumentNullException>(() => stringDict[null!]);
        Assert.Throws<ArgumentNullException>(() => stringDict[null!] = "piyo");


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict[1]);
        Assert.Throws<ObjectDisposedException>(() => dict[1] = 99);
    }

    [Fact]
    public void Keys()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        Assert.Equivalent(new int[] { 1, 2, 3 }, dict.Keys);
        Assert.Equal(3, dict.Keys.Count);

        Assert.True(dict.Keys.Contains(1));
        Assert.False(dict.Keys.Contains(4));

        var ints = new int[8];
        dict.Keys.CopyTo(ints, 1);
        Assert.Equivalent(new int[] { 0, 1, 2, 3, 0, 0, 0, 0 }, ints);

        var keys = dict.Keys;
        dict.Add(4, 8);
        Assert.Equivalent(new int[] { 1, 2, 3, 4 }, keys);
        dict.Remove(1);
        Assert.Equivalent(new int[] { 2, 3, 4 }, keys);


        var enumerator = keys.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.False(enumerator.MoveNext());
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary

        dict.Add(5, 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = keys.GetEnumerator();


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Keys);
        Assert.Throws<ObjectDisposedException>(() => keys.Count);
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
    }

    [Fact]
    public void Values()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        Assert.Equivalent(new int[] { 2, 4, 6 }, dict.Values);
        Assert.Equal(3, dict.Values.Count);

        Assert.False(dict.Values.Contains(1));
        Assert.True(dict.Values.Contains(4));

        var ints = new int[8];
        dict.Values.CopyTo(ints, 1);
        Assert.Equivalent(new int[] { 0, 2, 4, 6, 0, 0, 0, 0 }, ints);

        var values = dict.Values;
        dict.Add(4, 8);
        Assert.Equivalent(new int[] { 2, 4, 6, 8 }, values);
        dict.Remove(1);
        Assert.Equivalent(new int[] { 4, 6, 8 }, values);


        var enumerator = values.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.False(enumerator.MoveNext());
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary

        dict.Add(5, 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = values.GetEnumerator();


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Values);
        Assert.Throws<ObjectDisposedException>(() => values.Count);
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
    }

    [Fact]
    public void Add()
    {
        var dict = new ArrayPoolDictionary<int, int>();
        Assert.Empty(dict);

        dict.Add(1, 2);
        Assert.Equal(2, dict[1]);
        Assert.Single(dict);

        dict.Add(new(2, 4));
        Assert.Equal(4, dict[2]);
        Assert.Equal(2, dict.Count);

        Assert.Throws<ArgumentException>(() => dict.Add(1, 2));
        Assert.Throws<ArgumentException>(() => dict.Add(new(2, 4)));


        for (int i = 3; i <= 100; i++)
        {
            dict.Add(i, i * 2);
            Assert.Equivalent(Enumerable.Range(1, i).ToArray(), dict.Keys);
            Assert.Equivalent(Enumerable.Range(1, i).Select(i => i * 2).ToArray(), dict.Values);
        }


        var enumerator = dict.GetEnumerator();
        dict.Add(-1, -2);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Add(3, 6));
        Assert.Throws<ObjectDisposedException>(() => dict.Add(new(3, 6)));
    }

    [Fact]
    public void AsSpan()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        var span = ArrayPoolDictionary<int, int>.AsSpan(dict);
        Assert.Equivalent(new Dictionary<int, int> { { 1, 2 }, { 2, 4 }, { 3, 6 } }.ToArray(), span.ToArray());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.AsSpan(dict));
    }

    [Fact]
    public void Clear()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.Clear();
        Assert.Empty(dict);
        Assert.Equal(new KeyValuePair<int, int>[0], dict);
        Assert.Equal(new int[0], dict.Keys);
        Assert.Equal(new int[0], dict.Values);


        // should not throw any exceptions
        dict.Clear();


        dict.Add(1, 2);
        var enumerator = dict.GetEnumerator();
        dict.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Clear());
    }

    [Fact]
    public void ContainsKey()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.ContainsKey(1));
        Assert.True(dict.ContainsKey(2));
        Assert.True(dict.ContainsKey(3));
        Assert.False(dict.ContainsKey(4));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.ContainsKey(1));
    }

    [Fact]
    public void ContainsValue()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.ContainsValue(2));
        Assert.True(dict.ContainsValue(4));
        Assert.True(dict.ContainsValue(6));
        Assert.False(dict.ContainsValue(1));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.ContainsValue(1));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.EnsureCapacity(48);
        Assert.Equal(64, dict.Capacity);

        dict.EnsureCapacity(16);
        Assert.Equal(64, dict.Capacity);

        Assert.Throws<ArgumentOutOfRangeException>(() => dict.EnsureCapacity(-1));
        Assert.Throws<OutOfMemoryException>(() => dict.EnsureCapacity(int.MaxValue));


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.EnsureCapacity(16);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.EnsureCapacity(16));
    }

    public readonly struct DoubleIntEqualityComparer : IEqualityComparer<int>, IAlternateEqualityComparer<double, int>
    {
        public int Create(double alternate)
        {
            return (int)alternate;
        }

        public bool Equals(double alternate, int other)
        {
            return (int)alternate == other;
        }

        public bool Equals([AllowNull] int x, [AllowNull] int y)
        {
            return x == y;
        }

        public int GetHashCode(double alternate)
        {
            return ((int)alternate).GetHashCode();
        }

        public int GetHashCode([DisallowNull] int obj)
        {
            return obj.GetHashCode();
        }
    }

    [Fact]
    public void GetAlternateLookup()
    {
        var comparer = new DoubleIntEqualityComparer();
        var dict = new ArrayPoolDictionary<int, int>(comparer) { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        var lookup = dict.GetAlternateLookup<double>();
        Assert.Throws<InvalidOperationException>(() => dict.GetAlternateLookup<string>());

        Assert.True(ReferenceEquals(dict, lookup.Dictionary));

        Assert.Equal(2, lookup[1.0]);
        Assert.Equal(2, lookup[1.5]);
        Assert.Throws<KeyNotFoundException>(() => lookup[-1.0]);

        lookup[1.0] = 123;
        Assert.Equal(123, lookup[1.0]);
        Assert.Equal(123, dict[1]);
        lookup[4.0] = 456;
        Assert.Equal(456, lookup[4.0]);
        Assert.Equal(456, dict[4]);

        Assert.True(lookup.ContainsKey(3.0));
        Assert.False(lookup.ContainsKey(-1.0));

        Assert.True(lookup.Remove(4.0));
        Assert.False(lookup.ContainsKey(4.0));
        Assert.False(dict.ContainsKey(4));
        Assert.False(lookup.Remove(-1.0));

        {
            Assert.True(lookup.Remove(3.0, out var prevKey, out var prevValue));
            Assert.Equal(3, prevKey);
            Assert.Equal(6, prevValue);
        }

        Assert.True(lookup.TryAdd(3.0, 6));
        Assert.Equal(6, lookup[3.0]);
        Assert.Equal(6, dict[3]);
        Assert.False(lookup.TryAdd(3.0, 8));
        Assert.Equal(6, lookup[3.0]);
        Assert.Equal(6, dict[3]);

        {
            Assert.True(lookup.TryGetValue(3.0, out var value));
            Assert.Equal(6, value);
            Assert.False(lookup.TryGetValue(4.0, out value));

            Assert.True(lookup.TryGetValue(3.0, out var key, out value));
            Assert.Equal(3, key);
            Assert.Equal(6, value);
            Assert.False(lookup.TryGetValue(4.0, out key, out value));
        }


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup[5.0] = 10;
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup.Remove(5.0);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        {
            lookup.Remove(3.0, out var prevKey, out var prevValue);
        }
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup.TryAdd(5.0, 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        using var normalDict = new ArrayPoolDictionary<int, int>(dict);
        Assert.Throws<InvalidOperationException>(() => normalDict.GetAlternateLookup<double>());


#if NET9_0_OR_GREATER
        {
            using var stringDict = new ArrayPoolDictionary<string, string>();
            stringDict.Add("Alice", "Supernova");
            Assert.True(stringDict.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue("Alice", out var value));
            Assert.Equal("Supernova", value);
        }
#endif


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.GetAlternateLookup<double>());
    }

    [Fact]
    public void GetEnumerator()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        Assert.Equal(new KeyValuePair<int, int>[] { new(1, 2), new(2, 4), new(3, 6) }, dict);

        var enumerator = dict.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Add(4, 8);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        enumerator = dict.GetEnumerator();
        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
    }

    [Fact]
    public void GetValueRefOrAddDefault()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        for (int i = 0; i < 33; i++)
        {
            ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, i, out bool exists) = i * 3;

            Assert.Equal(1 <= i && i <= 3, exists);
            Assert.Equal(i * 3, dict[i]);
        }


        var enumerator = dict.GetEnumerator();
        ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 1, out _) = 123;
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 0, out bool exists) = 0);
    }

    [Fact]
    public void GetValueRefOrNullRef()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        for (int i = 0; i < 33; i++)
        {
            ref var value = ref ArrayPoolDictionary<int, int>.GetValueRefOrNullRef(dict, i);

            Assert.Equal(1 <= i && i <= 3, !Unsafe.IsNullRef(ref value));

            if (!Unsafe.IsNullRef(ref value))
            {
                value = i * 3;
                Assert.Equal(i * 3, dict[i]);
            }
        }


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 0, out bool exists) = 0);

    }

    [Fact]
    public void Remove()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.Remove(1));
        Assert.False(dict.ContainsKey(1));
        Assert.False(dict.Remove(1));

        Assert.True(dict.Remove(2, out var value));
        Assert.Equal(4, value);
        Assert.False(dict.Remove(2, out value));

        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.Remove(3);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Remove(3));
        Assert.Throws<ObjectDisposedException>(() => dict.Remove(3, out value));
    }

    [Fact]
    public void TrimExcess()
    {
        var dict = new ArrayPoolDictionary<int, int>(48);

        dict.TrimExcess();
        Assert.Equal(16, dict.Capacity);

        dict.TrimExcess(256);
        Assert.Equal(256, dict.Capacity);
        Assert.Throws<ArgumentOutOfRangeException>(() => dict.TrimExcess(-1));

        for (int i = 0; i < 64; i++)
        {
            dict.Add(i, i * 2);
        }

        dict.TrimExcess(dict.Count);
        Assert.Equal(64, dict.Capacity);


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.TrimExcess();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.TrimExcess());
        Assert.Throws<ObjectDisposedException>(() => dict.TrimExcess(1));
    }

    [Fact]
    public void TryAdd()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.TryAdd(4, 8));
        Assert.Equal(8, dict[4]);
        Assert.False(dict.TryAdd(1, 123));
        Assert.Equal(2, dict[1]);


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.TryAdd(-1, -1);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.TryAdd(5, 10));
    }

    [Fact]
    public void TryGetAlternateLookup()
    {
        var dict = new ArrayPoolDictionary<int, int>(new DoubleIntEqualityComparer()) { { 3, 6 } };

        Assert.True(dict.TryGetAlternateLookup<double>(out var lookup));
        Assert.True(ReferenceEquals(dict, lookup.Dictionary));
        Assert.Equal(6, lookup[3.0]);

        using var normalDict = new ArrayPoolDictionary<string, string>();
        Assert.False(dict.TryGetAlternateLookup<string>(out var stringLookup));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.TryGetAlternateLookup<double>(out lookup));
    }

    [Fact]
    public void TryGetValue()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.TryGetValue(1, out var value));
        Assert.Equal(2, value);

        Assert.False(dict.TryGetValue(-1, out value));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.TryGetValue(1, out value));
    }

    [Fact]
    public void Add_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.Add(new(4, 8));
        Assert.Equal(8, dict[4]);

        Assert.Throws<ArgumentException>(() => dict.Add(1, 123));
        Assert.Equal(2, dict[1]);


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Add(new(-1, -1));
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Add(new(-1, -1)));
    }

    [Fact]
    public void Contains_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.Contains(new(1, 2)));
        Assert.False(dict.Contains(new(2, -1)));
        Assert.False(dict.Contains(new(-1, 2)));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Contains(new(-1, -1)));
    }

    [Fact]
    public void CopyTo_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        var array = new KeyValuePair<int, int>[8];

        dict.CopyTo(array, 1);
        Assert.Equivalent(new KeyValuePair<int, int>[] {
            new(), new(1, 2), new(2, 4), new(3, 6), new(), new(), new(), new() }, array);

        Assert.Throws<ArgumentOutOfRangeException>(() => dict.CopyTo(array, -1));
        Assert.Throws<ArgumentException>(() => dict.CopyTo(array, 99));
        Assert.Throws<ArgumentException>(() => dict.CopyTo(array, 6));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.CopyTo(array, 1));
    }

    [Fact]
    public void Remove_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.True(dict.Remove(new KeyValuePair<int, int>(1, 2)));
        Assert.False(dict.ContainsKey(1));

        Assert.False(dict.Remove(new KeyValuePair<int, int>(1, 2)));
        Assert.False(dict.Remove(new KeyValuePair<int, int>(-1, 2)));
        Assert.False(dict.Remove(new KeyValuePair<int, int>(2, -1)));


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Remove(new KeyValuePair<int, int>(2, 4));
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => dict.Remove(new KeyValuePair<int, int>(-1, -1)));
    }

    [Fact]
    public void CopyTo_ICollection()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        var objectArray = new object[8];
        ICollection icollection = dict;

        icollection.CopyTo(objectArray, 1);
        Assert.Equivalent(new object?[] {
            null,
            new KeyValuePair<int, int>(1, 2),
            new KeyValuePair<int, int>(2, 4),
            new KeyValuePair<int, int>(3, 6),
            null,
            null,
            null,
            null
        }, objectArray);

        Assert.Throws<ArgumentOutOfRangeException>(() => icollection.CopyTo(objectArray, -1));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(objectArray, 99));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(objectArray, 6));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(new object[8, 8], 1));
        Assert.Throws<ArgumentException>(() => icollection.CopyTo(new string[8], 1));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => icollection.CopyTo(objectArray, 1));
    }

    [Fact]
    public void Add_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        idict.Add(4, 8);
        Assert.Equal(8, dict[4]);

        Assert.Throws<ArgumentNullException>(() => idict.Add(null!, 0));
        Assert.Throws<ArgumentException>(() => idict.Add("5", 1));
        Assert.Throws<ArgumentException>(() => idict.Add(5, "10"));
        Assert.Throws<ArgumentException>(() => idict.Add(4, 8));


        var enumerator = dict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        _ = enumerator.Current;

        idict.Add(5, 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => idict.Add(6, 12));
    }

    [Fact]
    public void Contains_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        Assert.True(idict.Contains(1));
        Assert.False(idict.Contains(4));
        Assert.False(idict.Contains("1"));

        Assert.Throws<ArgumentNullException>(() => idict.Contains(null!));


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => idict.Contains(1));
    }

    [Fact]
    public void GetEnumerator_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        Assert.Equivalent(new KeyValuePair<int, int>[] { new(1, 2), new(2, 4), new(3, 6) }, idict);

        var enumerator = idict.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.True(enumerator.MoveNext());
        // should not throw any exceptions
        _ = enumerator.Entry;
        _ = enumerator.Key;
        _ = enumerator.Value;
        Assert.True(enumerator.MoveNext());
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);

        enumerator.Reset();
        Assert.True(enumerator.MoveNext());
        idict.Add(4, 8);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Reset());

        enumerator = idict.GetEnumerator();


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => idict.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [Fact]
    public void Items_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        Assert.Equal(2, idict[1]);
        Assert.Null(idict[-1]);
        Assert.Null(idict["hoge"]);
        Assert.Throws<ArgumentNullException>(() => idict[null!]);

        idict[1] = 123;
        Assert.Equal(123, dict[1]);
        idict[4] = 456;
        Assert.Equal(456, dict[4]);

        Assert.Throws<ArgumentException>(() => idict["hoge"] = 123);
        Assert.Throws<ArgumentException>(() => idict[123] = "hoge");
        Assert.Throws<ArgumentNullException>(() => idict[null!] = 123);

        var enumerator = idict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        idict[1] = 123;
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => idict[1]);
        Assert.Throws<ObjectDisposedException>(() => idict[1] = 123);
    }

    [Fact]
    public void Remove_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        idict.Remove(1);
        Assert.False(dict.ContainsKey(1));

        // should not throw any exceptions
        idict.Remove(-1);
        idict.Remove("hoge");

        Assert.Throws<ArgumentNullException>(() => idict.Remove(null!));


        var enumerator = idict.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        idict.Remove(2);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);


        dict.Dispose();
        Assert.Throws<ObjectDisposedException>(() => idict.Remove(3));
    }

    [Fact]
    public void Monkey()
    {
        var rng = new Random(0);

        var expect = new Dictionary<int, int>();
        using var actual = new ArrayPoolDictionary<int, int>();

        for (int i = 0; i < 1024 * 1024; i++)
        {
            if (rng.NextDouble() < 0.25)
            {
                int key = rng.Next(1024);

                Assert.Equal(expect.Remove(key), actual.Remove(key));
            }
            else
            {
                int key = rng.Next(1024);

                Assert.Equal(expect.TryAdd(key, key), actual.TryAdd(key, key));
            }
        }

        Assert.Equivalent(expect, actual);
    }

    public record class FixedHashCode(int Value)
    {
        public override int GetHashCode()
        {
            return 0;
        }
    }
}
