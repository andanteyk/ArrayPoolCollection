using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolHashSetFormatterTests
{
    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolHashSet<int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes));

        var source = new ArrayPoolHashSet<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes)!;
        CollectionAssert.AreEquivalent(source.ToArray(), dest.ToArray());

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes)!;

            CollectionAssert.AreEquivalent(source.ToArray(), dest.ToArray());
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolHashSet<string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes));

        var source = new ArrayPoolHashSet<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes)!;
        CollectionAssert.AreEquivalent(source.ToArray(), dest.ToArray());

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes)!;
            CollectionAssert.AreEquivalent(source.ToArray(), dest.ToArray());
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new HashSetWrapper<int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<HashSetWrapper<int>>(bytes)!;

        CollectionAssert.AreEquivalent(source.Values.ToArray(), dest.Values.ToArray());
    }
}

[MemoryPackable]
public partial class HashSetWrapper<T>
{
    public ArrayPoolHashSet<T> Values = new();
}
