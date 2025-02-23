using System.Buffers;
using ArrayPoolCollection.Pool;
using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser]
public class BufferPoolBenchmark
{
    [Benchmark]
    public void ArrayPool()
    {
        var buffer = ArrayPool<int>.Shared.Rent(1024);
        ArrayPool<int>.Shared.Return(buffer);
    }

    [Benchmark]
    public void DebugPool()
    {
        var buffer = DebugArrayPool<int>.Shared.Rent(1024);
        DebugArrayPool<int>.Shared.Return(buffer);
    }

    [Benchmark]
    public void SlimPool()
    {
        var buffer = SlimArrayPool<int>.Shared.Rent(1024);
        SlimArrayPool<int>.Shared.Return(buffer);
    }
}
