using System.Collections;
using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

public class ArrayPoolBitsBenchmark
{
    private BitArray bitArray = new(1000);
    private ArrayPoolBits bits = new(1000);

    [Benchmark]
    public BitArray CopyBitArray()
    {
        return new(bitArray);
    }

    [Benchmark]
    public ArrayPoolBits CopyBitsPooled()
    {
        return new(bits);
    }

    [Benchmark]
    public bool FindBitArray()
    {
        return bitArray[256];
    }

    [Benchmark]
    public bool FindBitsPooled()
    {
        return bits[256];
    }

    [Benchmark]
    public void AddRemoveBitArray()
    {
        bitArray.Length++;
        bitArray[^1] = true;
        bitArray.Length--;
    }

    [Benchmark]
    public void AddRemoveBitsPooled()
    {
        bits.Add(false);
        bits.RemoveAt(bits.Count - 1);
    }

    [Benchmark]
    public void IterateBitArray()
    {
        foreach (var element in bitArray) { }
    }

    [Benchmark]
    public void IterateBitsPooled()
    {
        foreach (var element in bits) { }
    }
}
