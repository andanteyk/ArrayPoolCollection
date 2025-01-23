namespace ArrayPoolCollection.Tests;

[TestClass]
public class LinqExtensionTests
{
    [TestMethod]
    public void ToArrayPool()
    {
        using var array = Enumerable.Range(0, 6).ToArrayPool();
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5 }, array);
    }

    [TestMethod]
    public void ToArrayPoolList()
    {
        using var list = Enumerable.Range(0, 6).ToArrayPoolList();
        CollectionAssert.AreEqual(new int[] { 0, 1, 2, 3, 4, 5 }, list);
    }
}
