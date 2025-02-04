using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolHashSetTests
{
    [TestMethod]
    public void Count()
    {
        var set = new ArrayPoolHashSet<int>();

        Assert.AreEqual(0, set.Count);

        for (int i = 0; i < 100; i++)
        {
            set.Add(i);
            Assert.AreEqual(i + 1, set.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            set.Remove(i);
            Assert.AreEqual(99 - i, set.Count);
        }


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Count);
    }

    [TestMethod]
    public void Capacity()
    {
        var set = new ArrayPoolHashSet<int>(100);
        Assert.AreEqual(128, set.Capacity);


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Capacity);
    }

    [TestMethod]
    public void Comparer()
    {
        var set = new ArrayPoolHashSet<string>();
        Assert.AreEqual(EqualityComparer<string>.Default, set.Comparer);

        using var caseInsensitiveSet = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, caseInsensitiveSet.Comparer);

        caseInsensitiveSet.Add("alice");
        Assert.IsTrue(caseInsensitiveSet.TryGetValue("Alice", out _));
        Assert.IsTrue(caseInsensitiveSet.TryGetValue("ALICE", out _));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Comparer);
    }

    [TestMethod]
    public void Ctor()
    {
        var source = new string[] { "ALICE", "Alice", "Barbara", "Charlotte" };


        using var noParam = new ArrayPoolHashSet<string>();

        using var withCompaerer = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, withCompaerer.Comparer);

        using var withCapacity = new ArrayPoolHashSet<string>(100);
        Assert.AreEqual(128, withCapacity.Capacity);

        using var withCapacityAndCompaerer = new ArrayPoolHashSet<string>(12, StringComparer.OrdinalIgnoreCase);
        Assert.AreEqual(16, withCapacityAndCompaerer.Capacity);
        Assert.AreEqual(StringComparer.OrdinalIgnoreCase, withCapacityAndCompaerer.Comparer);

        using var withSource = new ArrayPoolHashSet<string>(source);
        foreach (var name in source)
        {
            Assert.IsTrue(withSource.Contains(name));
        }
        Assert.AreEqual(4, withSource.Count);

        using var withSourceAndComparer = new ArrayPoolHashSet<string>(source, StringComparer.OrdinalIgnoreCase);
        foreach (var name in source)
        {
            Assert.IsTrue(withSourceAndComparer.Contains(name));
        }
        Assert.AreEqual(3, withSourceAndComparer.Count);

        using var copy = new ArrayPoolHashSet<string>(withSourceAndComparer, StringComparer.OrdinalIgnoreCase);
        foreach (var name in source)
        {
            Assert.IsTrue(copy.Contains(name));
        }
        Assert.AreEqual(3, copy.Count);
    }

    [TestMethod]
    public void Add()
    {
        var set = new ArrayPoolHashSet<int>();

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(set.Add(i));
            Assert.IsTrue(set.Contains(i));
            Assert.AreEqual(i + 1, set.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.IsFalse(set.Add(i));
            Assert.IsTrue(set.Contains(i));
            Assert.AreEqual(100, set.Count);
        }


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Add(-1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Add(-1));
    }

    [TestMethod]
    public void AsSpan()
    {
        var set = new ArrayPoolHashSet<int>() { 1, 2, 3 };

        var span = ArrayPoolHashSet<int>.AsSpan(set);
        CollectionAssert.AreEquivalent(new int[] { 1, 2, 3 }, span.ToArray());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => ArrayPoolHashSet<int>.AsSpan(set));
    }

    [TestMethod]
    public void Clear()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);

        set.Clear();
        Assert.AreEqual(0, set.Count);


        set.Add(1);
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Clear();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Clear());
    }

    [TestMethod]
    public void Contains()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(set.Contains(i));
        }

        Assert.IsFalse(set.Contains(-1));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Contains(-1));
    }

    [TestMethod]
    public void CopyTo()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        set.CopyTo(buffer, 1);
        Assert.AreEqual(0, buffer[0]);

        var official = new HashSet<int>();
        for (int i = 1; i <= 100; i++)
        {
            official.Add(buffer[i]);
        }
        Assert.AreEqual(100, official.Count);

        // should not throw any exceptions
        set.CopyTo(buffer, 127, 1);


        Assert.ThrowsException<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, -1));
        Assert.ThrowsException<ArgumentException>(() => set.CopyTo(buffer, 128));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, -1, 64));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, 64, -1));
        Assert.ThrowsException<ArgumentException>(() => set.CopyTo(buffer, 127, 127));

        Assert.ThrowsException<ArgumentException>(() => set.CopyTo(buffer.AsSpan(1..2)));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.CopyTo(buffer));
    }

    [TestMethod]
    public void CreateSetComparer()
    {
        var comparer = ArrayPoolHashSet<string>.CreateSetComparer();

        using var a = new ArrayPoolHashSet<string>(Enumerable.Range(0, 100).Select(i => i.ToString()));
        using var b = new ArrayPoolHashSet<string>(Enumerable.Range(0, 100).Select(i => i.ToString()));

        Assert.IsTrue(comparer.Equals(a, b));
        Assert.IsTrue(comparer.GetHashCode(a) == comparer.GetHashCode(b));
        Assert.IsFalse(comparer.Equals(a, null));
        Assert.IsFalse(comparer.Equals(b, null));
        Assert.IsTrue(comparer.Equals(null, null));

        a.Add("101");

        Assert.IsFalse(comparer.Equals(a, b));
        Assert.IsFalse(comparer.GetHashCode(a) == comparer.GetHashCode(b));

        b.Add("101");

        Assert.IsTrue(comparer.Equals(a, b));
        Assert.IsTrue(comparer.GetHashCode(a) == comparer.GetHashCode(b));
    }

    [TestMethod]
    public void EnsureCapacity()
    {
        var set = new ArrayPoolHashSet<int>();

        Assert.AreEqual(256, set.EnsureCapacity(192));
        Assert.AreEqual(256, set.EnsureCapacity(1));

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => set.EnsureCapacity(-1));


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.EnsureCapacity(256);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.EnsureCapacity(1));
    }

    [TestMethod]
    public void ExceptWith()
    {
        var set = new ArrayPoolHashSet<int>();

        set.ExceptWith(Enumerable.Range(0, 100));
        Assert.AreEqual(0, set.Count);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);
        set.ExceptWith(set);
        Assert.AreEqual(0, set.Count);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);
        set.ExceptWith(Enumerable.Range(0, 50));
        Assert.AreEqual(50, set.Count);

        set.ExceptWith(Enumerable.Range(-50, 50));
        Assert.AreEqual(50, set.Count);

        set.ExceptWith([]);
        Assert.AreEqual(50, set.Count);


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.ExceptWith(Enumerable.Range(0, 100));
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.ExceptWith(Enumerable.Range(0, 50)));
    }

    [TestMethod]
    public void GetAlternateLookup()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());

        var lookup = set.GetAlternateLookup<double>();

        for (double i = 0; i < 100; i++)
        {
            Assert.IsTrue(lookup.Contains(i));
            Assert.IsTrue(lookup.Contains(i + 0.5));

            Assert.IsTrue(lookup.TryGetValue(i, out var actual));
            Assert.AreEqual((int)i, actual);
        }

        for (double i = 50; i < 150; i++)
        {
            Assert.AreEqual(i >= 100, lookup.Add(i));
        }

        for (double i = 100; i < 200; i++)
        {
            Assert.AreEqual(i < 150, lookup.Remove(i));
        }


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.ThrowsException<InvalidOperationException>(() => emptySet.GetAlternateLookup<double>());


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        lookup.Add(-1.0);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());

        enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        lookup.Remove(-1.0);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.GetAlternateLookup<double>());
        Assert.ThrowsException<ObjectDisposedException>(() => lookup.Add(1.0));
        Assert.ThrowsException<ObjectDisposedException>(() => lookup.Contains(1.0));
        Assert.ThrowsException<ObjectDisposedException>(() => lookup.TryGetValue(1.0, out _));
        Assert.ThrowsException<ObjectDisposedException>(() => lookup.Remove(1.0));
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());

        var official = new HashSet<int>();

        foreach (var element in set)
        {
            Assert.IsTrue(official.Add(element));
        }
        Assert.AreEqual(100, official.Count);


        var enumerator = set.GetEnumerator();
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(enumerator.MoveNext());
            Assert.AreEqual(i, enumerator.Current);
        }
        Assert.IsFalse(enumerator.MoveNext());
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.GetEnumerator());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Current);
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.ThrowsException<ObjectDisposedException>(() => enumerator.Reset());
    }

    [TestMethod]
    public void IntersectWith()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);

        set.IntersectWith(set);
        Assert.AreEqual(100, set.Count);


        using var emptySet = new ArrayPoolHashSet<int>();
        emptySet.IntersectWith(set);
        Assert.AreEqual(0, emptySet.Count);

        set.IntersectWith(emptySet);
        Assert.AreEqual(100, set.Count);


        set.IntersectWith(Enumerable.Range(0, 25));
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i < 25, set.Contains(i));
        }


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.IntersectWith(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void IsProperSubsetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsFalse(set.IsProperSubsetOf(Enumerable.Range(0, 100)));
        Assert.IsTrue(set.IsProperSubsetOf(Enumerable.Range(0, 101)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsFalse(set.IsProperSubsetOf(set));
        Assert.IsFalse(set.IsProperSubsetOf(emptySet));
        Assert.IsTrue(emptySet.IsProperSubsetOf(set));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.IsProperSubsetOf(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void IsProperSupersetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsFalse(set.IsProperSupersetOf(Enumerable.Range(0, 100)));
        Assert.IsTrue(set.IsProperSupersetOf(Enumerable.Range(0, 99)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsFalse(set.IsProperSupersetOf(set));
        Assert.IsTrue(set.IsProperSupersetOf(emptySet));
        Assert.IsFalse(emptySet.IsProperSupersetOf(set));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.IsProperSupersetOf(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void IsSubsetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsTrue(set.IsSubsetOf(Enumerable.Range(0, 100)));
        Assert.IsTrue(set.IsSubsetOf(Enumerable.Range(0, 101)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsTrue(set.IsSubsetOf(set));
        Assert.IsFalse(set.IsSubsetOf(emptySet));
        Assert.IsTrue(emptySet.IsSubsetOf(set));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.IsSubsetOf(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void IsSupersetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsTrue(set.IsSupersetOf(Enumerable.Range(0, 100)));
        Assert.IsTrue(set.IsSupersetOf(Enumerable.Range(0, 99)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsTrue(set.IsSupersetOf(set));
        Assert.IsTrue(set.IsSupersetOf(emptySet));
        Assert.IsFalse(emptySet.IsSupersetOf(set));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.IsSupersetOf(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void Overlaps()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsTrue(set.Overlaps(Enumerable.Range(0, 100)));
        Assert.IsTrue(set.Overlaps([0]));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsTrue(set.Overlaps(set));
        Assert.IsFalse(set.Overlaps(emptySet));
        Assert.IsFalse(emptySet.Overlaps(set));
        Assert.IsFalse(emptySet.Overlaps(set));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Overlaps(Enumerable.Range(0, 10)));
    }

    [TestMethod]
    public void Remove()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.IsFalse(set.Remove(-1));
        Assert.AreEqual(100, set.Count);

        for (int i = 0; i < 100; i++)
        {
            Assert.IsTrue(set.Remove(i));
            Assert.AreEqual(99 - i, set.Count);
        }


        set.Add(1);
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Remove(1);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.Remove(-1));
    }

    [TestMethod]
    public void RemoveWhere()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.AreEqual(50, set.RemoveWhere(i => i % 2 == 0));
        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(i % 2 != 0, set.Contains(i));
        }

        Assert.AreEqual(0, set.RemoveWhere(i => false));


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.RemoveWhere(i => i < 10);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.Current);
        Assert.ThrowsException<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.RemoveWhere(i => true));
    }

    [TestMethod]
    public void SetEquals()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.IsTrue(set.SetEquals(set));

        Assert.IsFalse(set.SetEquals(Enumerable.Range(0, 99)));
        Assert.IsTrue(set.SetEquals(Enumerable.Range(0, 100)));
        Assert.IsFalse(set.SetEquals(Enumerable.Range(0, 101)));

        var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsFalse(set.SetEquals(emptySet));
        Assert.IsFalse(emptySet.SetEquals(set));
        Assert.IsTrue(emptySet.SetEquals([]));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.SetEquals(set));
    }

    [TestMethod]
    public void SymmetricExceptWith()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        set.SymmetricExceptWith(set);
        Assert.AreEqual(0, set.Count);

        set.UnionWith(Enumerable.Range(0, 100));
        set.SymmetricExceptWith([]);
        Assert.AreEqual(100, set.Count);


        set.SymmetricExceptWith(Enumerable.Range(50, 100));
        for (int i = 0; i < 150; i++)
        {
            Assert.AreEqual(i < 50 || 100 <= i, set.Contains(i));
        }


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.SymmetricExceptWith(set));
    }

    [TestMethod]
    public void TrimExcess()
    {
        var set = new ArrayPoolHashSet<int>(100);
        set.TrimExcess();
        Assert.AreEqual(16, set.Capacity);

        set.TrimExcess(96);
        Assert.AreEqual(128, set.Capacity);


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.TrimExcess());

    }

    [TestMethod]
    public void TryGetAlternateLookup()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());
        Assert.IsTrue(set.TryGetAlternateLookup<double>(out var lookup));

        using var emptySet = new ArrayPoolHashSet<int>();
        Assert.IsFalse(emptySet.TryGetAlternateLookup<double>(out _));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.TryGetAlternateLookup<double>(out _));
    }

    [TestMethod]
    public void TryGetValue()
    {
        var set = new ArrayPoolHashSet<string>(["Alice", "Barbara", "Charlotte"], StringComparer.OrdinalIgnoreCase);

        Assert.IsTrue(set.TryGetValue("ALICE", out var actualValue));
        Assert.AreEqual("Alice", actualValue);

        Assert.IsFalse(set.TryGetValue("Diona", out _));


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.TryGetValue("alice", out _));
    }

    [TestMethod]
    public void UnionWith()
    {
        var set = new ArrayPoolHashSet<int>();

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);

        set.UnionWith(set);
        Assert.AreEqual(100, set.Count);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.AreEqual(100, set.Count);

        set.UnionWith([]);
        Assert.AreEqual(100, set.Count);


        set.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => set.UnionWith([]));
    }

    [TestMethod]
    public void Monkey()
    {
        var rng = new Random(0);

        var expect = new HashSet<int>();
        using var actual = new ArrayPoolHashSet<int>();

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

                Assert.AreEqual(expect.Add(key), actual.Add(key));
            }
        }

        CollectionAssert.AreEquivalent(expect.ToArray(), actual.ToArray());
    }

    [ConditionalTestMethod("HUGE")]
    public void Pathological()
    {
        var set = new ArrayPoolHashSet<ArrayPoolDictionaryTests.FixedHashCode>();

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.IsTrue(set.Add(new ArrayPoolDictionaryTests.FixedHashCode(i)));

            if (i % 1000 == 0)
            {
                Debug.WriteLine($"{i}");
            }
        }

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.IsTrue(set.TryGetValue(new ArrayPoolDictionaryTests.FixedHashCode(i), out var value));
            Assert.AreEqual(i, value.Value);
        }
    }

    [ConditionalTestMethod("HUGE")]
    public void Huge()
    {
        using var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, CollectionHelper.ArrayMaxLength), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());
        Assert.AreEqual(CollectionHelper.ArrayMaxLength, set.Count);
        Assert.AreEqual(CollectionHelper.ArrayMaxLength, set.Capacity);

        Assert.ThrowsException<OutOfMemoryException>(() => set.Add(-1));

        Assert.AreEqual(CollectionHelper.ArrayMaxLength, ArrayPoolHashSet<int>.AsSpan(set).Length);

        var buffer = new int[CollectionHelper.ArrayMaxLength];
        set.CopyTo(buffer);
        CollectionAssert.AreEquivalent(set.ToArray(), buffer);

        using var copy = new ArrayPoolHashSet<int>(set);
        var setComparer = ArrayPoolHashSet<int>.CreateSetComparer();
        Assert.IsTrue(setComparer.Equals(set, copy));

        Assert.AreEqual(CollectionHelper.ArrayMaxLength, set.EnsureCapacity(1));

        copy.ExceptWith(buffer);
        Assert.AreEqual(0, copy.Count);

        _ = set.GetAlternateLookup<double>();

        foreach (var element in set)
        {
            Assert.IsTrue(set.Contains(element));
        }

        copy.UnionWith(set);
        Assert.AreEqual(CollectionHelper.ArrayMaxLength, copy.Count);

        copy.IntersectWith(set);
        Assert.AreEqual(CollectionHelper.ArrayMaxLength, copy.Count);

        Assert.IsFalse(set.IsProperSubsetOf(copy));
        Assert.IsFalse(set.IsProperSupersetOf(copy));
        Assert.IsTrue(set.IsSubsetOf(copy));
        Assert.IsTrue(set.IsSupersetOf(copy));
        Assert.IsTrue(set.Overlaps(copy));
        Assert.IsTrue(set.SetEquals(copy));

        copy.Remove(0);
        Assert.AreEqual(CollectionHelper.ArrayMaxLength - 1, copy.Count);

        copy.RemoveWhere(i => i % 2 == 0);
        Assert.AreEqual(CollectionHelper.ArrayMaxLength / 2, copy.Count);

        copy.SymmetricExceptWith(set);
        Assert.AreEqual(0, copy.Count);

        set.TrimExcess();

        Assert.IsTrue(set.TryGetAlternateLookup<double>(out _));

        Assert.IsTrue(set.TryGetValue(0, out _));

        set.Clear();
    }
}
