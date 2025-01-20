using System.Buffers;

namespace ArrayPoolCollection.Tests;

[TestClass]
public class SegmentedArrayTests
{
    [TestMethod]
    public void Ctor()
    {
        // should not throw any exceptions
        using var segmentedArray = new SegmentedArray<int>(Span<int>.Empty);
    }

    [TestMethod]
    public void Add()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.Add(1);
        segmentedArray.Add(2);
        segmentedArray.Add(3);

        var result = segmentedArray.ToArray();
        CollectionAssert.AreEqual(new int[] { 1, 2, 3 }, result);
    }

    [TestMethod]
    public void AddRange_Enumerable()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));
        var result = segmentedArray.ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).ToArray(), result);

        segmentedArray.AddRange(Enumerable.Range(0, 49));
        result = segmentedArray.ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).Concat(Enumerable.Range(0, 49)).ToArray(), result);
    }

    [TestMethod]
    public void AddRange_Span()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        var argument = Enumerable.Range(0, 49).ToArray();
        segmentedArray.AddRange(argument.AsSpan());

        var result = segmentedArray.ToArray();
        CollectionAssert.AreEqual(argument, result);
    }

    [TestMethod]
    public void WriteToSpan()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var result = new int[64];
        segmentedArray.WriteToSpan(result);
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).ToArray(), result[..49]);
        Assert.AreEqual(0, result[49]);

        result = new int[64];
        try
        {
            segmentedArray.WriteToSpan(result.AsSpan(..32));
            Assert.Fail();
        }
        catch (ArgumentException) { }
    }

    [TestMethod]
    public void ToArrayPool()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var array = segmentedArray.ToArrayPool(out var span);
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).ToArray(), span.ToArray());

        ArrayPool<int>.Shared.Return(array);
    }

    [TestMethod]
    public void ToArray()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var array = segmentedArray.ToArray();
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).ToArray(), array);
    }


    [TestMethod]
    public void ToList()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var list = segmentedArray.ToList();
        CollectionAssert.AreEqual(Enumerable.Range(0, 49).ToList(), list);
    }

    [TestMethod]
    public void GetTotalLength()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        for (int i = 0; i < 256; i++)
        {
            segmentedArray.Add(i);

            Assert.AreEqual(i + 1, segmentedArray.GetTotalLength());
        }
    }
}
