using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolBitsFormatterTests
{
    [Fact]
    public void Serialize()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolBits>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes));

        var source = new ArrayPoolBits();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.NextDouble() < 0.5);

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolBits>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
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

        Assert.Equal(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class BitsWrapper
{
    public ArrayPoolBits Values = new();
}
