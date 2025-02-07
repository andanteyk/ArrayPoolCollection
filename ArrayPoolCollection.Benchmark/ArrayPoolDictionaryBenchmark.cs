using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolDictionaryBenchmark
{
    private Dictionary<int, int> intDictionary = Enumerable.Range(0, 1000).ToDictionary(i => i, i => i);
    private ArrayPoolDictionary<int, int> intPooled = Enumerable.Range(0, 1000).ToArrayPoolDictionary(i => i, i => i);

    private Dictionary<string, string> stringDictionary = Enumerable.Range(0, 1000).ToDictionary(i => i.ToString(), i => i.ToString());
    private ArrayPoolDictionary<string, string> stringPooled = Enumerable.Range(0, 1000).ToArrayPoolDictionary(i => i.ToString(), i => i.ToString());

    [Benchmark]
    public Dictionary<int, int> CopyIntDictionary()
    {
        return new(intDictionary);
    }

    [Benchmark]
    public ArrayPoolDictionary<int, int> CopyIntPooled()
    {
        return new(intPooled);
    }

    [Benchmark]
    public int FindIntDictionary()
    {
        return intDictionary[256];
    }

    [Benchmark]
    public int FindIntPooled()
    {
        return intPooled[256];
    }

    [Benchmark]
    public void AddRemoveIntDictionary()
    {
        intDictionary.Add(-1, -1);
        intDictionary.Remove(-1);
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Add(-1, -1);
        intPooled.Remove(-1);
    }

    [Benchmark]
    public void IterateIntDictionary()
    {
        foreach (var element in intDictionary) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public Dictionary<string, string> CopyStringDictionary()
    {
        return new(stringDictionary);
    }

    [Benchmark]
    public ArrayPoolDictionary<string, string> CopyStringPooled()
    {
        return new(stringPooled);
    }

    [Benchmark]
    public string FindStringDictionary()
    {
        return stringDictionary["256"];
    }

    [Benchmark]
    public string FindStringPooled()
    {
        return stringPooled["256"];
    }

    [Benchmark]
    public void AddRemoveStringDictionary()
    {
        stringDictionary.Add("-1", "-1");
        stringDictionary.Remove("-1");
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Add("-1", "-1");
        stringPooled.Remove("-1");
    }

    [Benchmark]
    public void IterateStringDictionary()
    {
        foreach (var element in stringDictionary) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
