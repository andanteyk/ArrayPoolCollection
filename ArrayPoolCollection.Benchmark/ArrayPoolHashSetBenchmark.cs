using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolHashSetBenchmark
{
    private HashSet<int> intHashSet = Enumerable.Range(0, 1000).ToHashSet();
    private ArrayPoolHashSet<int> intPooled = Enumerable.Range(0, 1000).ToArrayPoolHashSet();

    private HashSet<string> stringHashSet = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToHashSet();
    private ArrayPoolHashSet<string> stringPooled = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArrayPoolHashSet();

    [Benchmark]
    public HashSet<int> CopyIntHashSet()
    {
        return new(intHashSet);
    }

    [Benchmark]
    public ArrayPoolHashSet<int> CopyIntPooled()
    {
        return new(intPooled);
    }

    [Benchmark]
    public bool FindIntHashSet()
    {
        return intHashSet.Contains(256);
    }

    [Benchmark]
    public bool FindIntPooled()
    {
        return intPooled.Contains(256);
    }

    [Benchmark]
    public void AddRemoveIntHashSet()
    {
        intHashSet.Add(-1);
        intHashSet.Remove(-1);
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Add(-1);
        intPooled.Remove(-1);
    }

    [Benchmark]
    public void IterateIntHashSet()
    {
        foreach (var element in intHashSet) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public HashSet<string> CopyStringHashSet()
    {
        return new(stringHashSet);
    }

    [Benchmark]
    public ArrayPoolHashSet<string> CopyStringPooled()
    {
        return new(stringPooled);
    }

    [Benchmark]
    public bool FindStringHashSet()
    {
        return stringHashSet.Contains("256");
    }

    [Benchmark]
    public bool FindStringPooled()
    {
        return stringPooled.Contains("256");
    }

    [Benchmark]
    public void AddRemoveStringHashSet()
    {
        stringHashSet.Add("-1");
        stringHashSet.Remove("-1");
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Add("-1");
        stringPooled.Remove("-1");
    }

    [Benchmark]
    public void IterateStringHashSet()
    {
        foreach (var element in stringHashSet) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
