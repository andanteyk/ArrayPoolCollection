namespace ArrayPoolCollection.Tests;

[TestClass]
public class LinqExtensionTests
{
    [TestMethod]
    public void ToArrayPool()
    {
        using var array = Enumerable.Range(0, 6).ToArrayPool();

        int i = 0;
        foreach (var value in array)
        {
            Assert.AreEqual(i, value);
            i++;
        }
    }
}
