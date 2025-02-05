using System.Buffers;

namespace ArrayPoolCollection.Tests;

public class SegmentedArrayTests
{
    [Fact]
    public void Ctor()
    {
        // should not throw any exceptions
        using var segmentedArray = new SegmentedArray<int>(Span<int>.Empty);
    }

    [Fact]
    public void Add()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.Add(1);
        segmentedArray.Add(2);
        segmentedArray.Add(3);

        var result = segmentedArray.ToArray();
        Assert.Equal([1, 2, 3], result);
    }

    [Fact]
    public void AddRange_Enumerable()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));
        var result = segmentedArray.ToArray();
        Assert.Equal(Enumerable.Range(0, 49).ToArray(), result);

        segmentedArray.AddRange(Enumerable.Range(0, 49));
        result = segmentedArray.ToArray();
        Assert.Equal(Enumerable.Range(0, 49).Concat(Enumerable.Range(0, 49)).ToArray(), result);
    }

    [Fact]
    public void AddRange_Span()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        var argument = Enumerable.Range(0, 49).ToArray();
        segmentedArray.AddRange(argument.AsSpan());

        var result = segmentedArray.ToArray();
        Assert.Equal(argument, result);
    }

    [Fact]
    public void WriteToSpan()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var result = new int[64];
        segmentedArray.CopyTo(result);
        Assert.Equal(Enumerable.Range(0, 49).ToArray(), result[..49]);
        Assert.Equal(0, result[49]);

        result = new int[64];
        try
        {
            segmentedArray.CopyTo(result.AsSpan(..32));
            Assert.Fail();
        }
        catch (ArgumentException) { }
    }

    [Fact]
    public void ToArrayPool()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var array = segmentedArray.ToArrayPool(out var span);
        Assert.Equal(Enumerable.Range(0, 49).ToArray(), span.ToArray());

        ArrayPool<int>.Shared.Return(array);
    }

    [Fact]
    public void ToArray()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var array = segmentedArray.ToArray();
        Assert.Equal(Enumerable.Range(0, 49).ToArray(), array);
    }


    [Fact]
    public void ToList()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        segmentedArray.AddRange(Enumerable.Range(0, 49));

        var list = segmentedArray.ToList();
        Assert.Equal(Enumerable.Range(0, 49).ToList(), list);
    }

    [Fact]
    public void GetTotalLength()
    {
        var segmentStack = new SegmentedArray<int>.Stack16();
        using var segmentedArray = new SegmentedArray<int>(segmentStack.AsSpan());

        for (int i = 0; i < 256; i++)
        {
            segmentedArray.Add(i);

            Assert.Equal(i + 1, segmentedArray.GetTotalLength());
        }
    }
}
