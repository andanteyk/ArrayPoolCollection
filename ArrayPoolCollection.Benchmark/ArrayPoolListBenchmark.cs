using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolListBenchmark
{
    private List<int> intList = Enumerable.Range(0, 1000).ToList();
    private ArrayPoolList<int> intPooled = Enumerable.Range(0, 1000).ToArrayPoolList();

    private List<string> stringList = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();
    private ArrayPoolList<string> stringPooled = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToArrayPoolList();

    [Benchmark]
    public List<int> CopyIntList()
    {
        return new(intList);
    }

    [Benchmark]
    public ArrayPoolList<int> CopyIntPooled()
    {
        return new(intPooled);
    }

    [Benchmark]
    public int FindIntList()
    {
        return intList[256];
    }

    [Benchmark]
    public int FindIntPooled()
    {
        return intPooled[256];
    }

    [Benchmark]
    public void AddRemoveIntList()
    {
        intList.Add(-1);
        intList.RemoveAt(intList.Count - 1);
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Add(-1);
        intPooled.RemoveAt(intList.Count - 1);
    }

    [Benchmark]
    public void IterateIntList()
    {
        foreach (var element in intList) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public List<string> CopyStringList()
    {
        return new(stringList);
    }

    [Benchmark]
    public ArrayPoolList<string> CopyStringPooled()
    {
        return new(stringPooled);
    }

    [Benchmark]
    public string FindStringList()
    {
        return stringList[256];
    }

    [Benchmark]
    public string FindStringPooled()
    {
        return stringPooled[256];
    }

    [Benchmark]
    public void AddRemoveStringList()
    {
        stringList.Add("-1");
        stringList.RemoveAt(stringList.Count - 1);
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Add("-1");
        stringPooled.RemoveAt(stringPooled.Count - 1);
    }

    [Benchmark]
    public void IterateStringList()
    {
        foreach (var element in stringList) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
