using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolWrapperBenchmark
{
    private int[] intArray = Enumerable.Range(0, 1000).ToArray();
    private ArrayPoolWrapper<int> intPooled = Enumerable.Range(0, 1000).ToArrayPool();

    private string[] stringArray = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArray();
    private ArrayPoolWrapper<string> stringPooled = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArrayPool();

    [Benchmark]
    public int[] CopyIntArray()
    {
        return intArray.ToArray();
    }

    [Benchmark]
    public ArrayPoolWrapper<int> CopyIntPooled()
    {
        return intPooled.ToArrayPool();
    }

    [Benchmark]
    public bool FindIntArray()
    {
        return intArray.Contains(256);
    }

    [Benchmark]
    public bool FindIntPooled()
    {
        return intPooled.Contains(256);
    }

    [Benchmark]
    public void IterateIntArray()
    {
        foreach (var element in intArray) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public string[] CopyStringArray()
    {
        return stringArray.ToArray();
    }

    [Benchmark]
    public ArrayPoolWrapper<string> CopyStringPooled()
    {
        return stringPooled.ToArrayPool();
    }

    [Benchmark]
    public bool FindStringArray()
    {
        return stringArray.Contains("256");
    }

    [Benchmark]
    public bool FindStringPooled()
    {
        return stringPooled.Contains("256");
    }

    [Benchmark]
    public void IterateStringArray()
    {
        foreach (var element in stringArray) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
