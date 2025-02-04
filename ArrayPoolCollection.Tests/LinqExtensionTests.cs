namespace ArrayPoolCollection.Tests;

[TestClass]
public class LinqExtensionTests
{
    [TestMethod]
    public void ToArrayPool()
    {
        var sourceArray = new int[] { 0, 1, 2, 3, 4, 5 };
        using var fromArray = sourceArray.ToArrayPool();
        CollectionAssert.AreEqual(sourceArray, fromArray);

        var sourceHashSet = new HashSet<int>() { 1, 2, 3, 4, 5, 6 };
        using var fromHashset = sourceHashSet.ToArrayPool();
        CollectionAssert.AreEquivalent(sourceHashSet.ToArray(), fromHashset);

        using var fromEnumerable = Enumerable.Range(0, 6).ToArrayPool();
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5 }, fromEnumerable);
    }

    [TestMethod]
    public void ToArrayPoolList()
    {
        using var list = Enumerable.Range(0, 6).ToArrayPoolList();
        CollectionAssert.AreEqual(Enumerable.Range(0, 6).ToArray(), list);
    }

    [TestMethod]
    public void ToArrayPoolDictionary()
    {
        var sourceKvp = Enumerable.Range(0, 100).Select(i => new KeyValuePair<int, int>(i, i * 2));
        using var fromKvp = sourceKvp.ToArrayPoolDictionary();
        CollectionAssert.AreEquivalent(sourceKvp.ToArray(), fromKvp);

        var sourceTuple = Enumerable.Range(0, 100).Select(i => (i, i * 2));
        using var fromTuple = sourceTuple.ToArrayPoolDictionary();
        CollectionAssert.AreEquivalent(sourceKvp.ToArray(), fromTuple);

        var sourceKey = Enumerable.Range(0, 100);
        using var fromKey = sourceKey.ToArrayPoolDictionary(key => key);
        CollectionAssert.AreEquivalent(sourceKey.Select(i => new KeyValuePair<int, int>(i, i)).ToArray(), fromKey);

        using var fromKeyValue = sourceKey.ToArrayPoolDictionary(key => key, value => value * 2);
        CollectionAssert.AreEquivalent(sourceKvp.ToArray(), fromKeyValue);
    }

    [TestMethod]
    public void ToArrayPoolHashSet()
    {
        var source = Enumerable.Range(0, 100);
        var hashset = source.ToArrayPoolHashSet();
        CollectionAssert.AreEquivalent(source.ToArray(), hashset.ToArray());
    }
}
