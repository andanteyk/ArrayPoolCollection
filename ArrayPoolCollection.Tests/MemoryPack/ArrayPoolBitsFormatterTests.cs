using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolBitsFormatterTests
{
    [TestMethod]
    public void Serialize()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolBits>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes));

        var source = new ArrayPoolBits();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.NextDouble() < 0.5);

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new BitsWrapper();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.NextDouble() < 0.5);
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<BitsWrapper>(bytes)!;

        CollectionAssert.AreEqual(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class BitsWrapper
{
    public ArrayPoolBits Values = new();
}
