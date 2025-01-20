namespace ArrayPoolCollection.Tests;

[TestClass]
public class ArrayPoolWrapperTests
{
    [TestMethod]
    public void Ctor()
    {
        // should not throw any exceptions
        using var one = new ArrayPoolWrapper<int>(1);
        using var zero = new ArrayPoolWrapper<int>(0);

        Assert.ThrowsException<ArgumentOutOfRangeException>(() => new ArrayPoolWrapper<int>(-1));

        Assert.AreEqual(0, one[0]);
    }

    [TestMethod]
    public void AsSpan()
    {
        var array = new ArrayPoolWrapper<int>(6);
        Assert.AreEqual(6, array.AsSpan().Length);

        Span<int> span = array;
        Assert.AreEqual(6, span.Length);

        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array.AsSpan());
    }

    [TestMethod]
    public void Items()
    {
        var array = new ArrayPoolWrapper<int>(6);

        array[3] = 123;
        Assert.AreEqual(123, array[3]);

        Assert.ThrowsException<IndexOutOfRangeException>(() => array[-1]);
        Assert.ThrowsException<IndexOutOfRangeException>(() => array[123] = 1);

        array.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() => array[3]);
    }

    [TestMethod]
    public void Count()
    {
        using var array = new ArrayPoolWrapper<int>(6);
        Assert.AreEqual(6, array.Count);
    }

    [TestMethod]
    public void Contains()
    {
        using var array = new ArrayPoolWrapper<int>(6)
        {
            [4] = 123
        };

        Assert.IsTrue(array.Contains(123));
        Assert.IsFalse(array.Contains(456));
    }

    [TestMethod]
    public void CopyTo()
    {
        using var array = new ArrayPoolWrapper<int>(6);
        for (int i = 0; i < array.Count; i++)
        {
            array[i] = i + 1;
        }

        var dest = new int[10];
        array.CopyTo(dest, 2);

        CollectionAssert.AreEqual(new int[] { 0, 0, 1, 2, 3, 4, 5, 6, 0, 0 }, dest);
    }

    [TestMethod]
    public void Dispose()
    {
        var array = new ArrayPoolWrapper<int>(6);
        array.Dispose();

        // should not throw any exceptions
        array.Dispose();
    }

    [TestMethod]
    public void GetEnumerator()
    {
        var array = new ArrayPoolWrapper<int>(6);
        for (int i = 0; i < array.Count; i++)
        {
            array[i] = i + 1;
        }

        int expected = 1;
        foreach (var value in array)
        {
            Assert.AreEqual(expected, value);
            expected++;
        }
    }

    [TestMethod]
    public void IndexOf()
    {
        var array = new ArrayPoolWrapper<int>(6);
        for (int i = 0; i < array.Count; i++)
        {
            array[i] = i + 1;
        }

        Assert.AreEqual(2, array.IndexOf(3));
        Assert.AreEqual(-1, array.IndexOf(0));
    }
}
