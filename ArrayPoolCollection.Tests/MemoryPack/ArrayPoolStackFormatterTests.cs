using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolStackFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolStack<int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes));

        var source = new ArrayPoolStack<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Push(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<int>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolStack<string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes));

        var source = new ArrayPoolStack<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source.Push(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolStack<string>>(bytes);
            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new StackWrapper<int>();

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<StackWrapper<int>>(bytes)!;

        Assert.Null(dest.Values);
        Assert.Equal(source.Guard, dest.Guard);


        source.Values = new();

        for (int i = 0; i < 32; i++)
        {
            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<StackWrapper<int>>(bytes)!;

            Assert.Equal(source.Values, dest.Values);
            Assert.Equal(source.Guard, dest.Guard);

            source.Values.Push(rng.Next());
        }

        bytes = MemoryPackSerializer.Serialize(source);
        dest = MemoryPackSerializer.Deserialize<StackWrapper<int>>(bytes)!;

        Assert.Equal(source.Values, dest.Values);
        Assert.Equal(source.Guard, dest.Guard);
    }

    [Fact]
    public void Overwrite()
    {
        using var source = new ArrayPoolStack<int>(Enumerable.Range(0, 100));

        var bytes = MemoryPackSerializer.Serialize(source);

        var dest = new ArrayPoolStack<int>();
        MemoryPackSerializer.Deserialize(bytes, ref dest!);

        Assert.Equal(source, dest);

        dest.Dispose();
    }
}

[MemoryPackable]
public partial class StackWrapper<T>
{
    public ArrayPoolStack<T>? Values;
    public int Guard = 123456;
}

