using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolDictionaryTests
{
    [TestMethod]
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
            Assert.AreEqual(withCapacity.Capacity, 16);

            Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolDictionary<int, int>(-1));
            Assert.ThrowsException<OutOfMemoryException>(() => new ArrayPoolDictionary<int, int>(int.MaxValue));
        }

        // should not throw any exceptions
        {
            using var withCapacityAndComparer = new ArrayPoolDictionary<int, int>(0, comparer);
            using var withCapacityAndNullComparer = new ArrayPoolDictionary<int, int>(0, null);
        }

        {
            using var withSource = new ArrayPoolDictionary<int, int>(source);
            CollectionAssert.AreEquivalent(source, withSource);

            using var withSourceAndComparer = new ArrayPoolDictionary<int, int>(source, comparer);
            CollectionAssert.AreEquivalent(source, withSource);
        }

        {
            using var withSource = new ArrayPoolDictionary<int, int>(source.AsEnumerable());
            CollectionAssert.AreEquivalent(source, withSource);

            using var withSourceAndComparer = new ArrayPoolDictionary<int, int>(source.AsEnumerable(), comparer);
            CollectionAssert.AreEquivalent(source, withSource);
        }
    }

    [TestMethod]
    public void Comparer()
    {
        var valueComparer = EqualityComparer<int>.Default;
        var classComparer = StringComparer.OrdinalIgnoreCase;

        var valueWithNull = new ArrayPoolDictionary<int, int>();
        Assert.AreEqual(valueComparer, valueWithNull.Comparer);

        var valueWithComparer = new ArrayPoolDictionary<int, int>(valueComparer);
        Assert.AreEqual(valueComparer, valueWithComparer.Comparer);

        var classWithNull = new ArrayPoolDictionary<string, string>();
        Assert.AreEqual(EqualityComparer<string>.Default, classWithNull.Comparer);

        var classWithComparer = new ArrayPoolDictionary<string, string>(classComparer);
        Assert.AreEqual(classComparer, classWithComparer.Comparer);


        valueWithNull.Dispose();
        valueWithComparer.Dispose();
        classWithNull.Dispose();
        classWithComparer.Dispose();

        Assert.ThrowsException<ObjectDisposedException>(() => valueWithNull.Comparer);
        Assert.ThrowsException<ObjectDisposedException>(() => valueWithComparer.Comparer);
        Assert.ThrowsException<ObjectDisposedException>(() => classWithNull.Comparer);
        Assert.ThrowsException<ObjectDisposedException>(() => classWithComparer.Comparer);
    }

    [TestMethod]
    public void Capacity()
    {
        var dict = new ArrayPoolDictionary<int, int>(48);
        Assert.AreEqual(64, dict.Capacity);

        dict.EnsureCapacity(192);
        Assert.AreEqual(256, dict.Capacity);


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Capacity);
    }

    [TestMethod]
    public void Count()
    {
        var dict = new ArrayPoolDictionary<int, int>(32);
        Assert.AreEqual(0, dict.Count);

        dict.Add(1, 2);
        Assert.AreEqual(1, dict.Count);

        dict.Add(2, 4);
        Assert.AreEqual(2, dict.Count);

        dict.Remove(1);
        Assert.AreEqual(1, dict.Count);


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Count);
    }

    [TestMethod]
    public void Items()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        Assert.AreEqual(2, dict[1]);
        Assert.AreEqual(4, dict[2]);
        Assert.AreEqual(6, dict[3]);
        Assert.ThrowsException<KeyNotFoundException>(() => dict[4]);

        dict[4] = 8;
        Assert.AreEqual(8, dict[4]);

        dict[1] = 123;
        Assert.AreEqual(123, dict[1]);

        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        dict[1] = 1234;
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        using var stringDict = new ArrayPoolDictionary<string, string>() { { "hoge", "fuga" } };
        Assert.AreEqual("fuga", stringDict["hoge"]);
        Assert.ThrowsException<ArgumentNullException>(() => stringDict[null!]);
        Assert.ThrowsException<ArgumentNullException>(() => stringDict[null!] = "piyo");


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict[1]);
        Assert.ThrowsException<ObjectDisposedException>(() => dict[1] = 99);
    }

    [TestMethod]
    public void Keys()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        CollectionAssert.AreEquivalent(new int[] { 1, 2, 3 }, dict.Keys);
        Assert.AreEqual(3, dict.Keys.Count);

        Assert.IsTrue(dict.Keys.Contains(1));
        Assert.IsFalse(dict.Keys.Contains(4));

        var ints = new int[8];
        dict.Keys.CopyTo(ints, 1);
        CollectionAssert.AreEquivalent(new int[] { 0, 1, 2, 3, 0, 0, 0, 0 }, ints);

        var keys = dict.Keys;
        dict.Add(4, 8);
        CollectionAssert.AreEquivalent(new int[] { 1, 2, 3, 4 }, keys);
        dict.Remove(1);
        CollectionAssert.AreEquivalent(new int[] { 2, 3, 4 }, keys);


        var enumerator = keys.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
        enumerator.Reset();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary

        dict.Add(5, 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = keys.GetEnumerator();


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Keys);
        Assert.ThrowsException<ObjectDisposedException>(() => keys.Count);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
    }

    [TestMethod]
    public void Values()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        CollectionAssert.AreEquivalent(new int[] { 2, 4, 6 }, dict.Values);
        Assert.AreEqual(3, dict.Values.Count);

        Assert.IsFalse(dict.Values.Contains(1));
        Assert.IsTrue(dict.Values.Contains(4));

        var ints = new int[8];
        dict.Values.CopyTo(ints, 1);
        CollectionAssert.AreEquivalent(new int[] { 0, 2, 4, 6, 0, 0, 0, 0 }, ints);

        var values = dict.Values;
        dict.Add(4, 8);
        CollectionAssert.AreEquivalent(new int[] { 2, 4, 6, 8 }, values);
        dict.Remove(1);
        CollectionAssert.AreEquivalent(new int[] { 4, 6, 8 }, values);


        var enumerator = values.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsFalse(enumerator.MoveNext());
        enumerator.Reset();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current; // should not throw any exceptions. the return value may vary

        dict.Add(5, 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = values.GetEnumerator();


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Values);
        Assert.ThrowsException<ObjectDisposedException>(() => values.Count);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
    }

    [TestMethod]
    public void Add()
    {
        var dict = new ArrayPoolDictionary<int, int>();
        Assert.AreEqual(0, dict.Count);

        dict.Add(1, 2);
        Assert.AreEqual(2, dict[1]);
        Assert.AreEqual(1, dict.Count);

        dict.Add(new(2, 4));
        Assert.AreEqual(4, dict[2]);
        Assert.AreEqual(2, dict.Count);

        Assert.ThrowsException<ArgumentException>(() => dict.Add(1, 2));
        Assert.ThrowsException<ArgumentException>(() => dict.Add(new(2, 4)));


        for (int i = 3; i <= 100; i++)
        {
            dict.Add(i, i * 2);
            CollectionAssert.AreEquivalent(Enumerable.Range(1, i).ToArray(), dict.Keys);
            CollectionAssert.AreEquivalent(Enumerable.Range(1, i).Select(i => i * 2).ToArray(), dict.Values);
        }


        var enumerator = dict.GetEnumerator();
        dict.Add(-1, -2);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Add(3, 6));
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Add(new(3, 6)));
    }

    [TestMethod]
    public void AsSpan()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        var span = ArrayPoolDictionary<int, int>.AsSpan(dict);
        CollectionAssert.AreEquivalent(new Dictionary<int, int> { { 1, 2 }, { 2, 4 }, { 3, 6 } }.ToArray(), span.ToArray());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.AsSpan(dict));
    }

    [TestMethod]
    public void Clear()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.Clear();
        Assert.AreEqual(0, dict.Count);
        CollectionAssert.AreEqual(new KeyValuePair<int, int>[0], dict);
        CollectionAssert.AreEqual(new int[0], dict.Keys);
        CollectionAssert.AreEqual(new int[0], dict.Values);


        // should not throw any exceptions
        dict.Clear();


        dict.Add(1, 2);
        var enumerator = dict.GetEnumerator();
        dict.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Clear());
    }

    [TestMethod]
    public void ContainsKey()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.ContainsKey(1));
        Assert.IsTrue(dict.ContainsKey(2));
        Assert.IsTrue(dict.ContainsKey(3));
        Assert.IsFalse(dict.ContainsKey(4));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.ContainsKey(1));
    }

    [TestMethod]
    public void ContainsValue()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.ContainsValue(2));
        Assert.IsTrue(dict.ContainsValue(4));
        Assert.IsTrue(dict.ContainsValue(6));
        Assert.IsFalse(dict.ContainsValue(1));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.ContainsValue(1));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.EnsureCapacity(48);
        Assert.AreEqual(64, dict.Capacity);

        dict.EnsureCapacity(16);
        Assert.AreEqual(64, dict.Capacity);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => dict.EnsureCapacity(-1));
        Assert.ThrowsException<OutOfMemoryException>(() => dict.EnsureCapacity(int.MaxValue));


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.EnsureCapacity(16);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.EnsureCapacity(16));
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

    [TestMethod]
    public void GetAlternateLookup()
    {
        var comparer = new DoubleIntEqualityComparer();
        var dict = new ArrayPoolDictionary<int, int>(comparer) { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        var lookup = dict.GetAlternateLookup<double>();
        Assert.ThrowsException<InvalidOperationException>(() => dict.GetAlternateLookup<string>());

        Assert.IsTrue(ReferenceEquals(dict, lookup.Dictionary));

        Assert.AreEqual(2, lookup[1.0]);
        Assert.AreEqual(2, lookup[1.5]);
        Assert.ThrowsException<KeyNotFoundException>(() => lookup[-1.0]);

        lookup[1.0] = 123;
        Assert.AreEqual(123, lookup[1.0]);
        Assert.AreEqual(123, dict[1]);
        lookup[4.0] = 456;
        Assert.AreEqual(456, lookup[4.0]);
        Assert.AreEqual(456, dict[4]);

        Assert.IsTrue(lookup.ContainsKey(3.0));
        Assert.IsFalse(lookup.ContainsKey(-1.0));

        Assert.IsTrue(lookup.Remove(4.0));
        Assert.IsFalse(lookup.ContainsKey(4.0));
        Assert.IsFalse(dict.ContainsKey(4));
        Assert.IsFalse(lookup.Remove(-1.0));

        {
            Assert.IsTrue(lookup.Remove(3.0, out var prevKey, out var prevValue));
            Assert.AreEqual(3, prevKey);
            Assert.AreEqual(6, prevValue);
        }

        Assert.IsTrue(lookup.TryAdd(3.0, 6));
        Assert.AreEqual(6, lookup[3.0]);
        Assert.AreEqual(6, dict[3]);
        Assert.IsFalse(lookup.TryAdd(3.0, 8));
        Assert.AreEqual(6, lookup[3.0]);
        Assert.AreEqual(6, dict[3]);

        {
            Assert.IsTrue(lookup.TryGetValue(3.0, out var value));
            Assert.AreEqual(6, value);
            Assert.IsFalse(lookup.TryGetValue(4.0, out value));

            Assert.IsTrue(lookup.TryGetValue(3.0, out var key, out value));
            Assert.AreEqual(3, key);
            Assert.AreEqual(6, value);
            Assert.IsFalse(lookup.TryGetValue(4.0, out key, out value));
        }


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup[5.0] = 10;
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup.Remove(5.0);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        {
            lookup.Remove(3.0, out var prevKey, out var prevValue);
        }
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        lookup.TryAdd(5.0, 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        using var normalDict = new ArrayPoolDictionary<int, int>(dict);
        Assert.ThrowsException<InvalidOperationException>(() => normalDict.GetAlternateLookup<double>());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.GetAlternateLookup<double>());
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        CollectionAssert.AreEqual(new KeyValuePair<int, int>[] { new(1, 2), new(2, 4), new(3, 6) }, dict);

        var enumerator = dict.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);

        enumerator.Reset();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Add(4, 8);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        enumerator = dict.GetEnumerator();
        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Reset());
    }

    [TestMethod]
    public void GetValueRefOrAddDefault()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        for (int i = 0; i < 33; i++)
        {
            ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, i, out bool exists) = i * 3;

            Assert.AreEqual(1 <= i && i <= 3, exists);
            Assert.AreEqual(i * 3, dict[i]);
        }


        var enumerator = dict.GetEnumerator();
        ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 1, out _) = 123;
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 0, out bool exists) = 0);
    }

    [TestMethod]
    public void GetValueRefOrNullRef()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        for (int i = 0; i < 33; i++)
        {
            ref var value = ref ArrayPoolDictionary<int, int>.GetValueRefOrNullRef(dict, i);

            Assert.AreEqual(1 <= i && i <= 3, !Unsafe.IsNullRef(ref value));

            if (!Unsafe.IsNullRef(ref value))
            {
                value = i * 3;
                Assert.AreEqual(i * 3, dict[i]);
            }
        }


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolDictionary<int, int>.GetValueRefOrAddDefault(dict, 0, out bool exists) = 0);

    }

    [TestMethod]
    public void Remove()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.Remove(1));
        Assert.IsFalse(dict.ContainsKey(1));
        Assert.IsFalse(dict.Remove(1));

        Assert.IsTrue(dict.Remove(2, out var value));
        Assert.AreEqual(4, value);
        Assert.IsFalse(dict.Remove(2, out value));

        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.Remove(3);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Remove(3));
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Remove(3, out value));
    }

    [TestMethod]
    public void TrimExcess()
    {
        var dict = new ArrayPoolDictionary<int, int>(48);

        dict.TrimExcess();
        Assert.AreEqual(16, dict.Capacity);

        dict.TrimExcess(256);
        Assert.AreEqual(256, dict.Capacity);
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => dict.TrimExcess(-1));

        for (int i = 0; i < 64; i++)
        {
            dict.Add(i, i * 2);
        }

        dict.TrimExcess(dict.Count);
        Assert.AreEqual(64, dict.Capacity);


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;
        dict.TrimExcess();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.TrimExcess());
        Assert.ThrowsException<ObjectDisposedException>(() => dict.TrimExcess(1));
    }

    [TestMethod]
    public void TryAdd()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.TryAdd(4, 8));
        Assert.AreEqual(8, dict[4]);
        Assert.IsFalse(dict.TryAdd(1, 123));
        Assert.AreEqual(2, dict[1]);


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.TryAdd(-1, -1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.TryAdd(5, 10));
    }

    [TestMethod]
    public void TryGetAlternateLookup()
    {
        var dict = new ArrayPoolDictionary<int, int>(new DoubleIntEqualityComparer()) { { 3, 6 } };

        Assert.IsTrue(dict.TryGetAlternateLookup<double>(out var lookup));
        Assert.IsTrue(ReferenceEquals(dict, lookup.Dictionary));
        Assert.AreEqual(6, lookup[3.0]);

        using var normalDict = new ArrayPoolDictionary<string, string>();
        Assert.IsFalse(dict.TryGetAlternateLookup<string>(out var stringLookup));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.TryGetAlternateLookup<double>(out lookup));
    }

    [TestMethod]
    public void TryGetValue()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.TryGetValue(1, out var value));
        Assert.AreEqual(2, value);

        Assert.IsFalse(dict.TryGetValue(-1, out value));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.TryGetValue(1, out value));
    }

    [TestMethod]
    public void Add_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        dict.Add(new(4, 8));
        Assert.AreEqual(8, dict[4]);

        Assert.ThrowsException<ArgumentException>(() => dict.Add(1, 123));
        Assert.AreEqual(2, dict[1]);


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Add(new(-1, -1));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Add(new(-1, -1)));
    }

    [TestMethod]
    public void Contains_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.Contains(new(1, 2)));
        Assert.IsFalse(dict.Contains(new(2, -1)));
        Assert.IsFalse(dict.Contains(new(-1, 2)));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Contains(new(-1, -1)));
    }

    [TestMethod]
    public void CopyTo_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        var array = new KeyValuePair<int, int>[8];

        dict.CopyTo(array, 1);
        CollectionAssert.AreEquivalent(new KeyValuePair<int, int>[] {
            new(), new(1, 2), new(2, 4), new(3, 6), new(), new(), new(), new() }, array);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => dict.CopyTo(array, -1));
        Assert.ThrowsException<ArgumentException>(() => dict.CopyTo(array, 99));
        Assert.ThrowsException<ArgumentException>(() => dict.CopyTo(array, 6));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.CopyTo(array, 1));
    }

    [TestMethod]
    public void Remove_ICollectionT()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };

        Assert.IsTrue(dict.Remove(new KeyValuePair<int, int>(1, 2)));
        Assert.IsFalse(dict.ContainsKey(1));

        Assert.IsFalse(dict.Remove(new KeyValuePair<int, int>(1, 2)));
        Assert.IsFalse(dict.Remove(new KeyValuePair<int, int>(-1, 2)));
        Assert.IsFalse(dict.Remove(new KeyValuePair<int, int>(2, -1)));


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;

        dict.Remove(new KeyValuePair<int, int>(2, 4));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => dict.Remove(new KeyValuePair<int, int>(-1, -1)));
    }

    [TestMethod]
    public void CopyTo_ICollection()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        var objectArray = new object[8];
        ICollection icollection = dict;

        icollection.CopyTo(objectArray, 1);
        CollectionAssert.AreEquivalent(new object?[] {
            null,
            new KeyValuePair<int, int>(1, 2),
            new KeyValuePair<int, int>(2, 4),
            new KeyValuePair<int, int>(3, 6),
            null,
            null,
            null,
            null
        }, objectArray);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => icollection.CopyTo(objectArray, -1));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(objectArray, 99));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(objectArray, 6));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(new object[8, 8], 1));
        Assert.ThrowsException<ArgumentException>(() => icollection.CopyTo(new string[8], 1));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => icollection.CopyTo(objectArray, 1));
    }

    [TestMethod]
    public void Add_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        idict.Add(4, 8);
        Assert.AreEqual(8, dict[4]);

        Assert.ThrowsException<ArgumentNullException>(() => idict.Add(null!, 0));
        Assert.ThrowsException<ArgumentException>(() => idict.Add("5", 1));
        Assert.ThrowsException<ArgumentException>(() => idict.Add(5, "10"));
        Assert.ThrowsException<ArgumentException>(() => idict.Add(4, 8));


        var enumerator = dict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        _ = enumerator.Current;

        idict.Add(5, 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => idict.Add(6, 12));
    }

    [TestMethod]
    public void Contains_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        Assert.IsTrue(idict.Contains(1));
        Assert.IsFalse(idict.Contains(4));
        Assert.IsFalse(idict.Contains("1"));

        Assert.ThrowsException<ArgumentNullException>(() => idict.Contains(null!));


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => idict.Contains(1));
    }

    [TestMethod]
    public void GetEnumerator_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        CollectionAssert.AreEquivalent(new KeyValuePair<int, int>[] { new(1, 2), new(2, 4), new(3, 6) }, idict);

        var enumerator = idict.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Entry);
        Assert.IsTrue(enumerator.MoveNext());
        // should not throw any exceptions
        _ = enumerator.Entry;
        _ = enumerator.Key;
        _ = enumerator.Value;
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsTrue(enumerator.MoveNext());
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Entry);

        enumerator.Reset();
        Assert.IsTrue(enumerator.MoveNext());
        idict.Add(4, 8);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Entry);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Reset());

        enumerator = idict.GetEnumerator();


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => idict.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Items_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        Assert.AreEqual(2, idict[1]);
        Assert.IsNull(idict[-1]);
        Assert.IsNull(idict["hoge"]);
        Assert.ThrowsException<ArgumentNullException>(() => idict[null!]);

        idict[1] = 123;
        Assert.AreEqual(123, dict[1]);
        idict[4] = 456;
        Assert.AreEqual(456, dict[4]);

        Assert.ThrowsException<ArgumentException>(() => idict["hoge"] = 123);
        Assert.ThrowsException<ArgumentException>(() => idict[123] = "hoge");
        Assert.ThrowsException<ArgumentNullException>(() => idict[null!] = 123);

        var enumerator = idict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        idict[1] = 123;
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Entry);


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => idict[1]);
        Assert.ThrowsException<ObjectDisposedException>(() => idict[1] = 123);
    }

    [TestMethod]
    public void Remove_IDictionary()
    {
        var dict = new ArrayPoolDictionary<int, int>() { { 1, 2 }, { 2, 4 }, { 3, 6 } };
        IDictionary idict = dict;

        idict.Remove(1);
        Assert.IsFalse(dict.ContainsKey(1));

        // should not throw any exceptions
        idict.Remove(-1);
        idict.Remove("hoge");

        Assert.ThrowsException<ArgumentNullException>(() => idict.Remove(null!));


        var enumerator = idict.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        idict.Remove(2);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Entry);


        dict.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => idict.Remove(3));
    }

    [TestMethod]
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

                Assert.AreEqual(expect.Remove(key), actual.Remove(key));
            }
            else
            {
                int key = rng.Next(1024);

                Assert.AreEqual(expect.TryAdd(key, key), actual.TryAdd(key, key));
            }
        }

        CollectionAssert.AreEquivalent(expect, actual);
    }

    public record class FixedHashCode(int Value)
    {
        public override int GetHashCode()
        {
            return 0;
        }
    }

    [ConditionalTestMethod("HUGE")]
    public void Pathological()
    {
        var dict = new ArrayPoolDictionary<FixedHashCode, int>();

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.IsTrue(dict.TryAdd(new FixedHashCode(i), i));

            if (i % 1000 == 0)
            {
                Debug.WriteLine($"{i}");
            }
        }

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.IsTrue(dict.TryGetValue(new FixedHashCode(i), out var value));
            Assert.AreEqual(i, value);
        }
    }

    [ConditionalTestMethod("HUGE")]
    public void Huge()
    {
        var rng = new Random(0);

        var dict = new ArrayPoolDictionary<int, int>(CollectionHelper.ArrayMaxLength, new DoubleIntEqualityComparer());
        int i;
        for (i = 0; i < CollectionHelper.ArrayMaxLength; i++)
        {
            dict.Add(i, i);
        }

        dict[0] = 123;
        Assert.AreEqual(123, dict[0]);
        dict[0] = 0;

        i = 0;
        foreach (var key in dict.Keys)
        {
            Assert.AreEqual(i, key);
            i++;
        }

        i = 0;
        foreach (var value in dict.Values)
        {
            Assert.AreEqual(i, value);
            i++;
        }

        Assert.AreEqual(CollectionHelper.ArrayMaxLength, dict.Capacity);

        Assert.AreEqual(new DoubleIntEqualityComparer(), dict.Comparer);

        Assert.AreEqual(CollectionHelper.ArrayMaxLength, dict.Count);

        Assert.ThrowsException<OutOfMemoryException>(() => dict.Add(-1, -1));
        Assert.ThrowsException<OutOfMemoryException>(() => dict.Add(new(-1, -1)));

        Assert.AreEqual(CollectionHelper.ArrayMaxLength, ArrayPoolDictionary<int, int>.AsSpan(dict).Length);

        // dict.Clear();

        Assert.IsTrue(dict.Contains(new(1, 1)));

        Assert.IsTrue(dict.ContainsKey(1));

        Assert.IsTrue(dict.ContainsKey(CollectionHelper.ArrayMaxLength));

        Assert.ThrowsException<OutOfMemoryException>(() => dict.EnsureCapacity(int.MaxValue));

        var buffer = new KeyValuePair<int, int>[CollectionHelper.ArrayMaxLength];
        dict.CopyTo(buffer, 0);

        Assert.IsTrue(dict.GetAlternateLookup<double>().ContainsKey(1.0));
        Assert.IsTrue(dict.GetAlternateLookup<double>().TryGetValue(1.0, out _));
        Assert.ThrowsException<OutOfMemoryException>(() => dict.GetAlternateLookup<double>().TryAdd(-1.0, 2));

        foreach (var pair in dict)
        {
            Assert.IsTrue(pair.Key == pair.Value);
        }

        Assert.IsFalse(dict.Remove(-1));

        dict.TrimExcess();

        Assert.IsFalse(dict.TryAdd(-1, -1));

        Assert.IsTrue(dict.TryGetAlternateLookup<double>(out _));

        Assert.IsTrue(dict.TryGetValue(1, out _));
    }
}
