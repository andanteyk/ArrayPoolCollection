using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolPriorityQueueFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolPriorityQueue<int, int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes));

        var source = new ArrayPoolPriorityQueue<int, int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes)!;
        Assert.Equal(source.UnorderedItems, dest.UnorderedItems);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.Next(), rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes)!;

            Assert.Equal(source.UnorderedItems, dest.UnorderedItems);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolPriorityQueue<string, string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes));

        var source = new ArrayPoolPriorityQueue<string, string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes);
        Assert.Equal(source.UnorderedItems, dest!.UnorderedItems);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.NextDouble().ToString(), rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes);
            Assert.Equal(source.UnorderedItems, dest!.UnorderedItems);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new PriorityQueueWrapper<int, int>();

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<PriorityQueueWrapper<int, int>>(bytes)!;

        Assert.Null(dest.Values);
        Assert.Equal(source.Guard, dest.Guard);


        source.Values = new();

        for (int i = 0; i < 16; i++)
        {
            source.Values.Enqueue(rng.Next(), rng.Next());
        }

        bytes = MemoryPackSerializer.Serialize(source);
        dest = MemoryPackSerializer.Deserialize<PriorityQueueWrapper<int, int>>(bytes)!;

        Assert.Equal(source.Values.UnorderedItems, dest.Values!.UnorderedItems);
        Assert.Equal(source.Guard, dest.Guard);
    }

    [Fact]
    public void Overwrite()
    {
        using var source = new ArrayPoolPriorityQueue<int, int>(Enumerable.Range(0, 100).Select(i => (i, i)));

        var bytes = MemoryPackSerializer.Serialize(source);

        var dest = new ArrayPoolPriorityQueue<int, int>();
        MemoryPackSerializer.Deserialize(bytes, ref dest!);

        Assert.Equal(source.UnorderedItems, dest.UnorderedItems);

        dest.Dispose();
    }
}

[MemoryPackable]
public partial class PriorityQueueWrapper<TElement, TPriority>
{
    [MemoryPackAllowSerialize]
    public ArrayPoolPriorityQueue<TElement, TPriority>? Values;
    public int Guard = 123456;
}
