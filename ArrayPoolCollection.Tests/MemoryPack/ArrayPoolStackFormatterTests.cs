using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolStackFormatterTests
{
    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolStack<int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes));

        var source = new ArrayPoolStack<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Push(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolStack<string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes));

        var source = new ArrayPoolStack<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Push(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes);
            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new StackWrapper<int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Push(rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<StackWrapper<int>>(bytes)!;

        CollectionAssert.AreEqual(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class StackWrapper<T>
{
    public ArrayPoolStack<T> Values = new();
}

