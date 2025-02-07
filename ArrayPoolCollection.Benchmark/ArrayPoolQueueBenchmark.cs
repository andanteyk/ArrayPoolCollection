using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolQueueBenchmark
{
    private Queue<int> intQueue = new(Enumerable.Range(0, 1000));
    private ArrayPoolQueue<int> intPooled = new(Enumerable.Range(0, 1000));

    private Queue<string> stringQueue = new(Enumerable.Range(0, 1000).Select(i => i.ToString()));
    private ArrayPoolQueue<string> stringPooled = new(Enumerable.Range(0, 1000).Select(i => i.ToString()));

    [Benchmark]
    public Queue<int> CopyIntQueue()
    {
        return new(intQueue);
    }

    [Benchmark]
    public ArrayPoolQueue<int> CopyIntPooled()
    {
        return new(intPooled);
    }

    [Benchmark]
    public bool FindIntQueue()
    {
        return intQueue.Contains(256);
    }

    [Benchmark]
    public bool FindIntPooled()
    {
        return intPooled.Contains(256);
    }

    [Benchmark]
    public void AddRemoveIntQueue()
    {
        intQueue.Enqueue(-1);
        intQueue.Dequeue();
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Enqueue(-1);
        intPooled.Dequeue();
    }

    [Benchmark]
    public void IterateIntQueue()
    {
        foreach (var element in intQueue) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled) { }
    }

    [Benchmark]
    public Queue<string> CopyStringQueue()
    {
        return new(stringQueue);
    }

    [Benchmark]
    public ArrayPoolQueue<string> CopyStringPooled()
    {
        return new(stringPooled);
    }

    [Benchmark]
    public bool FindStringQueue()
    {
        return stringQueue.Contains("256");
    }

    [Benchmark]
    public bool FindStringPooled()
    {
        return stringPooled.Contains("256");
    }

    [Benchmark]
    public void AddRemoveStringQueue()
    {
        stringQueue.Enqueue("-1");
        stringQueue.Dequeue();
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Enqueue("-1");
        stringPooled.Dequeue();
    }

    [Benchmark]
    public void IterateStringQueue()
    {
        foreach (var element in stringQueue) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled) { }
    }
}
