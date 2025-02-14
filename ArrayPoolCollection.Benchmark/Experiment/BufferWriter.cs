using System.Buffers;
using System.IO.Pipelines;
using ArrayPoolCollection.Buffers;
using BenchmarkDotNet.Attributes;
using MemoryPack;

namespace ArrayPoolCollection.Benchmark.Experiment;

[MemoryDiagnoser]
public class BufferWriter
{
    private readonly TestClass Source = new TestClass(123, "Alice", "GDD", Enumerable.Range(0, 1000000).Select(i => i.ToString()).ToArray());

    [Benchmark]
    public TestClass ByteArray()
    {
        var bytes = MemoryPackSerializer.Serialize(Source);
        return MemoryPackSerializer.Deserialize<TestClass>(bytes)!;
    }

    [Benchmark]
    public async ValueTask<TestClass> Pipe()
    {
        var pipe = new Pipe();
        MemoryPackSerializer.Serialize(pipe.Writer, Source);
        await pipe.Writer.CompleteAsync();
        var readResult = await pipe.Reader.ReadAsync();
        return MemoryPackSerializer.Deserialize<TestClass>(readResult.Buffer)!;
    }

    [Benchmark]
    public TestClass ArrayBufferWriter()
    {
        var writer = new ArrayBufferWriter<byte>();
        MemoryPackSerializer.Serialize(writer, Source);
        return MemoryPackSerializer.Deserialize<TestClass>(writer.WrittenSpan)!;
    }

    [Benchmark]
    public TestClass ArrayPoolBufferWriter()
    {
        using var writer = new ArrayPoolBufferWriter<byte>(1024);
        MemoryPackSerializer.Serialize(writer, Source);
        return MemoryPackSerializer.Deserialize<TestClass>(writer.WrittenSpan)!;
    }

    [Benchmark]
    public TestClass ArrayPoolSegmentedBufferWriter()
    {
        using var writer = new ArrayPoolSegmentedBufferWriter<byte>(1024);
        MemoryPackSerializer.Serialize(writer, Source);
        return MemoryPackSerializer.Deserialize<TestClass>(writer.GetWrittenSequence())!;
    }

    [Benchmark]
    public TestClass With()
    {
        return Source with { Name = new string(Source.Name), Address = new string(Source.Address), Status = Source.Status.Select(i => new string(i)).ToArray() };
    }
}

[MemoryPackable]
public partial record class TestClass(int Id, string Name, string Address, string[] Status)
{
}
