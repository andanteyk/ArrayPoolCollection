using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class ArrayPoolPriorityQueueBenchmark
{
    private PriorityQueue<int, int> intPriorityQueue = new(Enumerable.Range(0, 1000).Select(i => (i, i)));
    private ArrayPoolPriorityQueue<int, int> intPooled = new(Enumerable.Range(0, 1000).Select(i => (i, i)));

    private PriorityQueue<string, int> stringPriorityQueue = new(Enumerable.Range(0, 1000).Select(i => (i.ToString(), i)));
    private ArrayPoolPriorityQueue<string, int> stringPooled = new(Enumerable.Range(0, 1000).Select(i => (i.ToString(), i)));

    [Benchmark]
    public PriorityQueue<int, int> CopyIntPriorityQueue()
    {
        return new(intPriorityQueue.UnorderedItems);
    }

    [Benchmark]
    public ArrayPoolPriorityQueue<int, int> CopyIntPooled()
    {
        return new(intPooled.UnorderedItems);
    }

    [Benchmark]
    public void AddRemoveIntPriorityQueue()
    {
        intPriorityQueue.Enqueue(-1, -1);
        intPriorityQueue.Dequeue();
    }

    [Benchmark]
    public void AddRemoveIntPooled()
    {
        intPooled.Enqueue(-1, -1);
        intPooled.Dequeue();
    }

    [Benchmark]
    public void IterateIntPriorityQueue()
    {
        foreach (var element in intPriorityQueue.UnorderedItems) { }
    }

    [Benchmark]
    public void IterateIntPooled()
    {
        foreach (var element in intPooled.UnorderedItems) { }
    }

    [Benchmark]
    public PriorityQueue<string, int> CopyStringPriorityQueue()
    {
        return new(stringPriorityQueue.UnorderedItems);
    }

    [Benchmark]
    public ArrayPoolPriorityQueue<string, int> CopyStringPooled()
    {
        return new(stringPooled.UnorderedItems);
    }

    [Benchmark]
    public void AddRemoveStringPriorityQueue()
    {
        stringPriorityQueue.Enqueue("-1", -1);
        stringPriorityQueue.Dequeue();
    }

    [Benchmark]
    public void AddRemoveStringPooled()
    {
        stringPooled.Enqueue("-1", -1);
        stringPooled.Dequeue();
    }

    [Benchmark]
    public void IterateStringPriorityQueue()
    {
        foreach (var element in stringPriorityQueue.UnorderedItems) { }
    }

    [Benchmark]
    public void IterateStringPooled()
    {
        foreach (var element in stringPooled.UnorderedItems) { }
    }
}
