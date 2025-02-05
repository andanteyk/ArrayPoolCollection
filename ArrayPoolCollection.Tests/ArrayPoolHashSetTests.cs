using System.Diagnostics;

namespace ArrayPoolCollection.Tests;

public class ArrayPoolHashSetTests
{
    [Fact]
    public void Count()
    {
        var set = new ArrayPoolHashSet<int>();

        Assert.Empty(set);

        for (int i = 0; i < 100; i++)
        {
            set.Add(i);
            Assert.Equal(i + 1, set.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            set.Remove(i);
            Assert.Equal(99 - i, set.Count);
        }


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Count);
    }

    [Fact]
    public void Capacity()
    {
        var set = new ArrayPoolHashSet<int>(100);
        Assert.Equal(128, set.Capacity);


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Capacity);
    }

    [Fact]
    public void Comparer()
    {
        var set = new ArrayPoolHashSet<string>();
        Assert.Equal(EqualityComparer<string>.Default, set.Comparer);

        using var caseInsensitiveSet = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, caseInsensitiveSet.Comparer);

        caseInsensitiveSet.Add("alice");
        Assert.True(caseInsensitiveSet.TryGetValue("Alice", out _));
        Assert.True(caseInsensitiveSet.TryGetValue("ALICE", out _));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Comparer);
    }

    [Fact]
    public void Ctor()
    {
        var source = new string[] { "ALICE", "Alice", "Barbara", "Charlotte" };


        using var noParam = new ArrayPoolHashSet<string>();

        using var withCompaerer = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, withCompaerer.Comparer);

        using var withCapacity = new ArrayPoolHashSet<string>(100);
        Assert.Equal(128, withCapacity.Capacity);

        using var withCapacityAndCompaerer = new ArrayPoolHashSet<string>(12, StringComparer.OrdinalIgnoreCase);
        Assert.Equal(16, withCapacityAndCompaerer.Capacity);
        Assert.Equal(StringComparer.OrdinalIgnoreCase, withCapacityAndCompaerer.Comparer);

        using var withSource = new ArrayPoolHashSet<string>(source);
        foreach (var name in source)
        {
            Assert.True(withSource.Contains(name));
        }
        Assert.Equal(4, withSource.Count);

        using var withSourceAndComparer = new ArrayPoolHashSet<string>(source, StringComparer.OrdinalIgnoreCase);
        foreach (var name in source)
        {
            Assert.True(withSourceAndComparer.Contains(name));
        }
        Assert.Equal(3, withSourceAndComparer.Count);

        using var copy = new ArrayPoolHashSet<string>(withSourceAndComparer, StringComparer.OrdinalIgnoreCase);
        foreach (var name in source)
        {
            Assert.True(copy.Contains(name));
        }
        Assert.Equal(3, copy.Count);
    }

    [Fact]
    public void Add()
    {
        var set = new ArrayPoolHashSet<int>();

        for (int i = 0; i < 100; i++)
        {
            Assert.True(set.Add(i));
            Assert.True(set.Contains(i));
            Assert.Equal(i + 1, set.Count);
        }

        for (int i = 0; i < 100; i++)
        {
            Assert.False(set.Add(i));
            Assert.True(set.Contains(i));
            Assert.Equal(100, set.Count);
        }


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Add(-1);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Add(-1));
    }

    [Fact]
    public void AsSpan()
    {
        var set = new ArrayPoolHashSet<int>() { 1, 2, 3 };

        var span = ArrayPoolHashSet<int>.AsSpan(set);
        Assert.Equivalent(new int[] { 1, 2, 3 }, span.ToArray());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => ArrayPoolHashSet<int>.AsSpan(set));
    }

    [Fact]
    public void Clear()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);

        set.Clear();
        Assert.Empty(set);


        set.Add(1);
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Clear();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Clear());
    }

    [Fact]
    public void Contains()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        for (int i = 0; i < 100; i++)
        {
            Assert.True(set.Contains(i));
        }

        Assert.False(set.Contains(-1));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Contains(-1));
    }

    [Fact]
    public void CopyTo()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        var buffer = new int[128];

        set.CopyTo(buffer, 1);
        Assert.Equal(0, buffer[0]);

        var official = new HashSet<int>();
        for (int i = 1; i <= 100; i++)
        {
            official.Add(buffer[i]);
        }
        Assert.Equal(100, official.Count);

        // should not throw any exceptions
        set.CopyTo(buffer, 127, 1);


        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, -1));
        Assert.Throws<ArgumentException>(() => set.CopyTo(buffer, 128));

        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, -1, 64));
        Assert.Throws<ArgumentOutOfRangeException>(() => set.CopyTo(buffer, 64, -1));
        Assert.Throws<ArgumentException>(() => set.CopyTo(buffer, 127, 127));

        Assert.Throws<ArgumentException>(() => set.CopyTo(buffer.AsSpan(1..2)));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.CopyTo(buffer));
    }

    [Fact]
    public void CreateSetComparer()
    {
        var comparer = ArrayPoolHashSet<string>.CreateSetComparer();

        using var a = new ArrayPoolHashSet<string>(Enumerable.Range(0, 100).Select(i => i.ToString()));
        using var b = new ArrayPoolHashSet<string>(Enumerable.Range(0, 100).Select(i => i.ToString()));

        Assert.True(comparer.Equals(a, b));
        Assert.True(comparer.GetHashCode(a) == comparer.GetHashCode(b));
        Assert.False(comparer.Equals(a, null));
        Assert.False(comparer.Equals(b, null));
        Assert.True(comparer.Equals(null, null));

        a.Add("101");

        Assert.False(comparer.Equals(a, b));
        Assert.False(comparer.GetHashCode(a) == comparer.GetHashCode(b));

        b.Add("101");

        Assert.True(comparer.Equals(a, b));
        Assert.True(comparer.GetHashCode(a) == comparer.GetHashCode(b));
    }

    [Fact]
    public void EnsureCapacity()
    {
        var set = new ArrayPoolHashSet<int>();

        Assert.Equal(256, set.EnsureCapacity(192));
        Assert.Equal(256, set.EnsureCapacity(1));

        Assert.Throws<ArgumentOutOfRangeException>(() => set.EnsureCapacity(-1));


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.EnsureCapacity(256);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.EnsureCapacity(1));
    }

    [Fact]
    public void ExceptWith()
    {
        var set = new ArrayPoolHashSet<int>();

        set.ExceptWith(Enumerable.Range(0, 100));
        Assert.Empty(set);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);
        set.ExceptWith(set);
        Assert.Empty(set);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);
        set.ExceptWith(Enumerable.Range(0, 50));
        Assert.Equal(50, set.Count);

        set.ExceptWith(Enumerable.Range(-50, 50));
        Assert.Equal(50, set.Count);

        set.ExceptWith([]);
        Assert.Equal(50, set.Count);


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.ExceptWith(Enumerable.Range(0, 100));
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.ExceptWith(Enumerable.Range(0, 50)));
    }

    [Fact]
    public void GetAlternateLookup()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());

        var lookup = set.GetAlternateLookup<double>();

        for (double i = 0; i < 100; i++)
        {
            Assert.True(lookup.Contains(i));
            Assert.True(lookup.Contains(i + 0.5));

            Assert.True(lookup.TryGetValue(i, out var actual));
            Assert.Equal((int)i, actual);
        }

        for (double i = 50; i < 150; i++)
        {
            Assert.Equal(i >= 100, lookup.Add(i));
        }

        for (double i = 100; i < 200; i++)
        {
            Assert.Equal(i < 150, lookup.Remove(i));
        }


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.Throws<InvalidOperationException>(() => emptySet.GetAlternateLookup<double>());


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        lookup.Add(-1.0);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());

        enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        lookup.Remove(-1.0);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


#if NET9_0_OR_GREATER
        {
            var stringSet = new ArrayPoolHashSet<string> { "Alice", "Barbara", "Charlotte" };
            Assert.True(stringSet.TryGetAlternateLookup<ReadOnlySpan<char>>(out var alternate));
            Assert.True(alternate.TryGetValue("Alice", out var value));
            Assert.Equal("Alice", value);
        }
#endif


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.GetAlternateLookup<double>());
        Assert.Throws<ObjectDisposedException>(() => lookup.Add(1.0));
        Assert.Throws<ObjectDisposedException>(() => lookup.Contains(1.0));
        Assert.Throws<ObjectDisposedException>(() => lookup.TryGetValue(1.0, out _));
        Assert.Throws<ObjectDisposedException>(() => lookup.Remove(1.0));
    }

    [Fact]
    public void GetEnumerator()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());

        var official = new HashSet<int>();

        foreach (var element in set)
        {
            Assert.True(official.Add(element));
        }
        Assert.Equal(100, official.Count);


        var enumerator = set.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        for (int i = 0; i < 100; i++)
        {
            Assert.True(enumerator.MoveNext());
            Assert.Equal(i, enumerator.Current);
        }
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);


        enumerator.Reset();


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.GetEnumerator());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
    }

    [Fact]
    public void IntersectWith()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);

        set.IntersectWith(set);
        Assert.Equal(100, set.Count);


        using var emptySet = new ArrayPoolHashSet<int>();
        emptySet.IntersectWith(set);
        Assert.Empty(emptySet);

        set.IntersectWith(emptySet);
        Assert.Equal(100, set.Count);


        set.IntersectWith(Enumerable.Range(0, 25));
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i < 25, set.Contains(i));
        }


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.IntersectWith(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void IsProperSubsetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.False(set.IsProperSubsetOf(Enumerable.Range(0, 100)));
        Assert.True(set.IsProperSubsetOf(Enumerable.Range(0, 101)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.False(set.IsProperSubsetOf(set));
        Assert.False(set.IsProperSubsetOf(emptySet));
        Assert.True(emptySet.IsProperSubsetOf(set));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.IsProperSubsetOf(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void IsProperSupersetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.False(set.IsProperSupersetOf(Enumerable.Range(0, 100)));
        Assert.True(set.IsProperSupersetOf(Enumerable.Range(0, 99)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.False(set.IsProperSupersetOf(set));
        Assert.True(set.IsProperSupersetOf(emptySet));
        Assert.False(emptySet.IsProperSupersetOf(set));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.IsProperSupersetOf(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void IsSubsetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.True(set.IsSubsetOf(Enumerable.Range(0, 100)));
        Assert.True(set.IsSubsetOf(Enumerable.Range(0, 101)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.True(set.IsSubsetOf(set));
        Assert.False(set.IsSubsetOf(emptySet));
        Assert.True(emptySet.IsSubsetOf(set));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.IsSubsetOf(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void IsSupersetOf()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.True(set.IsSupersetOf(Enumerable.Range(0, 100)));
        Assert.True(set.IsSupersetOf(Enumerable.Range(0, 99)));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.True(set.IsSupersetOf(set));
        Assert.True(set.IsSupersetOf(emptySet));
        Assert.False(emptySet.IsSupersetOf(set));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.IsSupersetOf(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void Overlaps()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.True(set.Overlaps(Enumerable.Range(0, 100)));
        Assert.True(set.Overlaps([0]));


        var emptySet = new ArrayPoolHashSet<int>();
        Assert.True(set.Overlaps(set));
        Assert.False(set.Overlaps(emptySet));
        Assert.False(emptySet.Overlaps(set));
        Assert.False(emptySet.Overlaps(set));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Overlaps(Enumerable.Range(0, 10)));
    }

    [Fact]
    public void Remove()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.False(set.Remove(-1));
        Assert.Equal(100, set.Count);

        for (int i = 0; i < 100; i++)
        {
            Assert.True(set.Remove(i));
            Assert.Equal(99 - i, set.Count);
        }


        set.Add(1);
        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.Remove(1);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.Remove(-1));
    }

    [Fact]
    public void RemoveWhere()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));

        Assert.Equal(50, set.RemoveWhere(i => i % 2 == 0));
        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(i % 2 != 0, set.Contains(i));
        }

        Assert.Equal(0, set.RemoveWhere(i => false));


        var enumerator = set.GetEnumerator();
        enumerator.MoveNext();
        set.RemoveWhere(i => i < 10);
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.RemoveWhere(i => true));
    }

    [Fact]
    public void SetEquals()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        Assert.True(set.SetEquals(set));

        Assert.False(set.SetEquals(Enumerable.Range(0, 99)));
        Assert.True(set.SetEquals(Enumerable.Range(0, 100)));
        Assert.False(set.SetEquals(Enumerable.Range(0, 101)));

        var emptySet = new ArrayPoolHashSet<int>();
        Assert.False(set.SetEquals(emptySet));
        Assert.False(emptySet.SetEquals(set));
        Assert.True(emptySet.SetEquals([]));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.SetEquals(set));
    }

    [Fact]
    public void SymmetricExceptWith()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100));
        set.SymmetricExceptWith(set);
        Assert.Empty(set);

        set.UnionWith(Enumerable.Range(0, 100));
        set.SymmetricExceptWith([]);
        Assert.Equal(100, set.Count);


        set.SymmetricExceptWith(Enumerable.Range(50, 100));
        for (int i = 0; i < 150; i++)
        {
            Assert.Equal(i < 50 || 100 <= i, set.Contains(i));
        }


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.SymmetricExceptWith(set));
    }

    [Fact]
    public void TrimExcess()
    {
        var set = new ArrayPoolHashSet<int>(100);
        set.TrimExcess();
        Assert.Equal(16, set.Capacity);

        set.TrimExcess(96);
        Assert.Equal(128, set.Capacity);


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.TrimExcess());

    }

    [Fact]
    public void TryGetAlternateLookup()
    {
        var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, 100), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());
        Assert.True(set.TryGetAlternateLookup<double>(out var lookup));

        using var emptySet = new ArrayPoolHashSet<int>();
        Assert.False(emptySet.TryGetAlternateLookup<double>(out _));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.TryGetAlternateLookup<double>(out _));
    }

    [Fact]
    public void TryGetValue()
    {
        var set = new ArrayPoolHashSet<string>(["Alice", "Barbara", "Charlotte"], StringComparer.OrdinalIgnoreCase);

        Assert.True(set.TryGetValue("ALICE", out var actualValue));
        Assert.Equal("Alice", actualValue);

        Assert.False(set.TryGetValue("Diona", out _));


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.TryGetValue("alice", out _));
    }

    [Fact]
    public void UnionWith()
    {
        var set = new ArrayPoolHashSet<int>();

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);

        set.UnionWith(set);
        Assert.Equal(100, set.Count);

        set.UnionWith(Enumerable.Range(0, 100));
        Assert.Equal(100, set.Count);

        set.UnionWith([]);
        Assert.Equal(100, set.Count);


        set.Dispose();
        Assert.Throws<ObjectDisposedException>(() => set.UnionWith([]));
    }

    [Fact]
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

                Assert.Equal(expect.Remove(key), actual.Remove(key));
            }
            else
            {
                int key = rng.Next(1024);

                Assert.Equal(expect.Add(key), actual.Add(key));
            }
        }

        Assert.Equivalent(expect.ToArray(), actual.ToArray());
    }

    // TODO
    //[ConditionalFact("HUGE")]
    public void Pathological()
    {
        var set = new ArrayPoolHashSet<ArrayPoolDictionaryTests.FixedHashCode>();

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.True(set.Add(new ArrayPoolDictionaryTests.FixedHashCode(i)));

            if (i % 1000 == 0)
            {
                Debug.WriteLine($"{i}");
            }
        }

        for (int i = 0; i < 1 << 24; i++)
        {
            Assert.True(set.TryGetValue(new ArrayPoolDictionaryTests.FixedHashCode(i), out var value));
            Assert.Equal(i, value.Value);
        }
    }

    // TODO
    //[ConditionalFact("HUGE")]
    public void Huge()
    {
        using var set = new ArrayPoolHashSet<int>(Enumerable.Range(0, CollectionHelper.ArrayMaxLength), new ArrayPoolDictionaryTests.DoubleIntEqualityComparer());
        Assert.Equal(CollectionHelper.ArrayMaxLength, set.Count);
        Assert.Equal(CollectionHelper.ArrayMaxLength, set.Capacity);

        Assert.Throws<OutOfMemoryException>(() => set.Add(-1));

        Assert.Equal(CollectionHelper.ArrayMaxLength, ArrayPoolHashSet<int>.AsSpan(set).Length);

        var buffer = new int[CollectionHelper.ArrayMaxLength];
        set.CopyTo(buffer);
        Assert.Equivalent(set.ToArray(), buffer);

        using var copy = new ArrayPoolHashSet<int>(set);
        var setComparer = ArrayPoolHashSet<int>.CreateSetComparer();
        Assert.True(setComparer.Equals(set, copy));

        Assert.Equal(CollectionHelper.ArrayMaxLength, set.EnsureCapacity(1));

        copy.ExceptWith(buffer);
        Assert.Empty(copy);

        _ = set.GetAlternateLookup<double>();

        foreach (var element in set)
        {
            Assert.True(set.Contains(element));
        }

        copy.UnionWith(set);
        Assert.Equal(CollectionHelper.ArrayMaxLength, copy.Count);

        copy.IntersectWith(set);
        Assert.Equal(CollectionHelper.ArrayMaxLength, copy.Count);

        Assert.False(set.IsProperSubsetOf(copy));
        Assert.False(set.IsProperSupersetOf(copy));
        Assert.True(set.IsSubsetOf(copy));
        Assert.True(set.IsSupersetOf(copy));
        Assert.True(set.Overlaps(copy));
        Assert.True(set.SetEquals(copy));

        copy.Remove(0);
        Assert.Equal(CollectionHelper.ArrayMaxLength - 1, copy.Count);

        copy.RemoveWhere(i => i % 2 == 0);
        Assert.Equal(CollectionHelper.ArrayMaxLength / 2, copy.Count);

        copy.SymmetricExceptWith(set);
        Assert.Empty(copy);

        set.TrimExcess();

        Assert.True(set.TryGetAlternateLookup<double>(out _));

        Assert.True(set.TryGetValue(0, out _));

        set.Clear();
    }
}
