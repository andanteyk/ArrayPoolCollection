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

    private int[][] TempBuffer = new int[1024][];

    [Benchmark]
    public void ArrayPool1k()
    {
        for (int i = 0; i < TempBuffer.Length; i++)
        {
            TempBuffer[i] = ArrayPool<int>.Shared.Rent(1024);
        }

        for (int i = 0; i < TempBuffer.Length; i++)
        {
            ArrayPool<int>.Shared.Return(TempBuffer[i]);
        }
    }

    [Benchmark]
    public void DebugPool1k()
    {
        for (int i = 0; i < TempBuffer.Length; i++)
        {
            TempBuffer[i] = DebugArrayPool<int>.Shared.Rent(1024);
        }

        for (int i = 0; i < TempBuffer.Length; i++)
        {
            DebugArrayPool<int>.Shared.Return(TempBuffer[i]);
        }
    }

    [Benchmark]
    public void SlimPool1k()
    {
        for (int i = 0; i < TempBuffer.Length; i++)
        {
            TempBuffer[i] = SlimArrayPool<int>.Shared.Rent(1024);
        }

        for (int i = 0; i < TempBuffer.Length; i++)
        {
            SlimArrayPool<int>.Shared.Return(TempBuffer[i]);
        }
    }
}
