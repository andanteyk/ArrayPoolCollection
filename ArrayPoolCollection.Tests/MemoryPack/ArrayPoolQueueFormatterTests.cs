using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolQueueFormatterTests
{
    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolQueue<int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes));

        var source = new ArrayPoolQueue<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<int>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolQueue<string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes));

        var source = new ArrayPoolQueue<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Enqueue(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolQueue<string>>(bytes);
            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new QueueWrapper<int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Enqueue(rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<QueueWrapper<int>>(bytes)!;

        CollectionAssert.AreEqual(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class QueueWrapper<T>
{
    public ArrayPoolQueue<T> Values = new();
}

