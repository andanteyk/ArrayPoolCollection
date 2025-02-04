using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolListFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolList<int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolList<int>>(bytes));

        var source = new ArrayPoolList<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolList<int>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolList<int>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolList<string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolList<string>>(bytes));

        var source = new ArrayPoolList<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolList<string>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolList<string>>(bytes);
            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new ListWrapper<int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ListWrapper<int>>(bytes)!;

        Assert.Equal(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class ListWrapper<T>
{
    public ArrayPoolList<T> Values = new();
}
