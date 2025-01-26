using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolPriorityQueueFormatterTests
{
    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolPriorityQueue<int, int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes));

        var source = new ArrayPoolPriorityQueue<int, int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes)!;
        CollectionAssert.AreEqual(source.UnorderedItems, dest.UnorderedItems);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.Next(), rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<int, int>>(bytes)!;

            CollectionAssert.AreEqual(source.UnorderedItems, dest.UnorderedItems);
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolPriorityQueue<string, string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes));

        var source = new ArrayPoolPriorityQueue<string, string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes);
        CollectionAssert.AreEqual(source.UnorderedItems, dest!.UnorderedItems);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.NextDouble().ToString(), rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolPriorityQueue<string, string>>(bytes);
            CollectionAssert.AreEqual(source.UnorderedItems, dest!.UnorderedItems);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new PriorityQueueWrapper<int, int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Enqueue(rng.Next(), rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<PriorityQueueWrapper<int, int>>(bytes)!;

        CollectionAssert.AreEqual(source.Values.UnorderedItems, dest.Values.UnorderedItems);
    }
}

[MemoryPackable]
public partial class PriorityQueueWrapper<TElement, TPriority>
{
    [MemoryPackAllowSerialize]
    public ArrayPoolPriorityQueue<TElement, TPriority> Values = new();
}
