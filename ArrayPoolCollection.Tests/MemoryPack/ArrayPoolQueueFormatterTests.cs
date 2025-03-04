using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolQueueFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolQueue<int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes));

        var source = new ArrayPoolQueue<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolQueue<string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes));

        var source = new ArrayPoolQueue<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes);
            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new QueueWrapper<int>();

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<QueueWrapper<int>>(bytes)!;

        Assert.Null(dest.Values);
        Assert.Equal(source.Guard, dest.Guard);


        source.Values = new();

        for (int i = 0; i < 32; i++)
        {
            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<QueueWrapper<int>>(bytes)!;

            Assert.Equal(source.Values, dest.Values);
            Assert.Equal(source.Guard, dest.Guard);

            source.Values.Enqueue(rng.Next());
        }

        bytes = MemoryPackSerializer.Serialize(source);
        dest = MemoryPackSerializer.Deserialize<QueueWrapper<int>>(bytes)!;

        Assert.Equal(source.Values, dest.Values);
        Assert.Equal(source.Guard, dest.Guard);
    }

    [Fact]
    public void Overwrite()
    {
        using var source = new ArrayPoolQueue<int>(Enumerable.Range(0, 100));

        var bytes = MemoryPackSerializer.Serialize(source);

        var dest = new ArrayPoolQueue<int>();
        MemoryPackSerializer.Deserialize(bytes, ref dest!);

        Assert.Equal(source, dest);

        dest.Dispose();
    }
}

[MemoryPackable]
public partial class QueueWrapper<T>
{
    public ArrayPoolQueue<T>? Values;
    public int Guard = 123456;
}

