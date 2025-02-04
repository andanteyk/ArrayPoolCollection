namespace ArrayPoolCollection.Tests;

public class LinqExtensionTests
{
    [Fact]
    public void ToArrayPool()
    {
        var sourceArray = new int[] { 0, 1, 2, 3, 4, 5 };
        using var fromArray = sourceArray.ToArrayPool();
        Assert.Equal(sourceArray, fromArray);

        var sourceHashSet = new HashSet<int>() { 1, 2, 3, 4, 5, 6 };
        using var fromHashset = sourceHashSet.ToArrayPool();
        Assert.Equivalent(sourceHashSet.ToArray(), fromHashset);

        using var fromEnumerable = Enumerable.Range(0, 6).ToArrayPool();
        Assert.Equal(new int[] { 0, 1, 2, 3, 4, 5 }, fromEnumerable);
    }

    [Fact]
    public void ToArrayPoolList()
    {
        using var list = Enumerable.Range(0, 6).ToArrayPoolList();
        Assert.Equal(Enumerable.Range(0, 6).ToArray(), list);
    }

    [Fact]
    public void ToArrayPoolDictionary()
    {
        var sourceKvp = Enumerable.Range(0, 100).Select(i => new KeyValuePair<int, int>(i, i * 2));
        using var fromKvp = sourceKvp.ToArrayPoolDictionary();
        Assert.Equivalent(sourceKvp.ToArray(), fromKvp);

        var sourceTuple = Enumerable.Range(0, 100).Select(i => (i, i * 2));
        using var fromTuple = sourceTuple.ToArrayPoolDictionary();
        Assert.Equivalent(sourceKvp.ToArray(), fromTuple);

        var sourceKey = Enumerable.Range(0, 100);
        using var fromKey = sourceKey.ToArrayPoolDictionary(key => key);
        Assert.Equivalent(sourceKey.Select(i => new KeyValuePair<int, int>(i, i)).ToArray(), fromKey);

        using var fromKeyValue = sourceKey.ToArrayPoolDictionary(key => key, value => value * 2);
        Assert.Equivalent(sourceKvp.ToArray(), fromKeyValue);
    }

    [Fact]
    public void ToArrayPoolHashSet()
    {
        var source = Enumerable.Range(0, 100);
        var hashset = source.ToArrayPoolHashSet();
        Assert.Equivalent(source.ToArray(), hashset.ToArray());
    }
}
