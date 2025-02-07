using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolStackBenchmark
{
    private Stack<int> intStack = new(Enumerable.Range(0, 1000));
    private ArrayPoolStack<int> intPooled = new(Enumerable.Range(0, 1000));

    private Stack<string> stringStack = new(Enumerable.Range(0, 1000).Select(i => i.ToString()));
    private ArrayPoolStack<string> stringPooled = new(Enumerable.Range(0, 1000).Select(i => i.ToString()));

    [Benchmark]
    public Stack<int> CopyIntStack()
    {
        return new(intStack);
    }

    [Benchmark]
    public ArrayPoolStack<int> CopyIntPooled()
    {
        return new(intPooled);
    }

    [Benchmark]
    public bool FindIntStack()
    {
        return intStack.Contains(256);
    }

    [Benchmark]
    public bool FindIntPooled()
    {
        return intPooled.Contains(256);
    }

    [Benchmark]
    public void AddRemoveIntStack()
    {
        intStack.Push(-1);
        intStack.Pop();
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Push(-1);
        intPooled.Pop();
    }

    [Benchmark]
    public void IterateIntStack()
    {
        foreach (var element in intStack) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public Stack<string> CopyStringStack()
    {
        return new(stringStack);
    }

    [Benchmark]
    public ArrayPoolStack<string> CopyStringPooled()
    {
        return new(stringPooled);
    }

    [Benchmark]
    public bool FindStringStack()
    {
        return stringStack.Contains("256");
    }

    [Benchmark]
    public bool FindStringPooled()
    {
        return stringPooled.Contains("256");
    }

    [Benchmark]
    public void AddRemoveStringStack()
    {
        stringStack.Push("-1");
        stringStack.Pop();
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Push("-1");
        stringPooled.Pop();
    }

    [Benchmark]
    public void IterateStringStack()
    {
        foreach (var element in stringStack) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
