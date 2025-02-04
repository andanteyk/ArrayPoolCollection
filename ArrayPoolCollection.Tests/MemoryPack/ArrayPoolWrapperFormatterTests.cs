using System.Runtime.InteropServices;
using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolWrapperFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolWrapper<int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolWrapper<int>>(bytes));

        for (int i = 0; i <= 1024; i++)
        {
            var source = new ArrayPoolWrapper<int>(i);
            rng.NextBytes(MemoryMarshal.AsBytes(source.AsSpan()));

            bytes = MemoryPackSerializer.Serialize(source);
            var dest = MemoryPackSerializer.Deserialize<ArrayPoolWrapper<int>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolWrapper<string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolWrapper<string>>(bytes));

        for (int i = 0; i <= 1024; i++)
        {
            var source = new ArrayPoolWrapper<string>(i);
            for (int k = 0; k < i; k++)
            {
                source[k] = rng.NextDouble().ToString();
            }

            bytes = MemoryPackSerializer.Serialize(source);
            var dest = MemoryPackSerializer.Deserialize<ArrayPoolWrapper<string>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new ArrayWrapper<int>();
        rng.NextBytes(MemoryMarshal.AsBytes(source.Values.AsSpan()));

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayWrapper<int>>(bytes)!;

        Assert.Equal(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class ArrayWrapper<T>
{
    public ArrayPoolWrapper<T> Values = new(16);
}
