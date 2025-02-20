using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolHashSetFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolHashSet<int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes));

        var source = new ArrayPoolHashSet<int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes)!;
        Assert.Equivalent(source.ToArray(), dest.ToArray());

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.Next());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<int>>(bytes)!;

            Assert.Equivalent(source.ToArray(), dest.ToArray());
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolHashSet<string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes));

        var source = new ArrayPoolHashSet<string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes)!;
        Assert.Equivalent(source.ToArray(), dest.ToArray());

        for (int i = 0; i < 1024; i++)
        {
            source.Add(rng.NextDouble().ToString());

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolHashSet<string>>(bytes)!;
            Assert.Equivalent(source.ToArray(), dest.ToArray());
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new HashSetWrapper<int>();

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<HashSetWrapper<int>>(bytes)!;

        Assert.Null(dest.Values);
        Assert.Equal(source.Guard, dest.Guard);


        source.Values = new();

        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.Next());
        }

        bytes = MemoryPackSerializer.Serialize(source);
        dest = MemoryPackSerializer.Deserialize<HashSetWrapper<int>>(bytes)!;

        Assert.Equivalent(source.Values.ToArray(), dest.Values!.ToArray());
        Assert.Equal(source.Guard, dest.Guard);
    }

    [Fact]
    public void Overwrite()
    {
        var source = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase) { { "Alice" } };

        var bytes = MemoryPackSerializer.Serialize(source);

        var dest = new ArrayPoolHashSet<string>(StringComparer.OrdinalIgnoreCase);
        MemoryPackSerializer.Deserialize(bytes, ref dest);

        Assert.Equal(StringComparer.OrdinalIgnoreCase, dest!.Comparer);
        Assert.True(dest.Contains("alice"));
    }
}

[MemoryPackable]
public partial class HashSetWrapper<T>
{
    public ArrayPoolHashSet<T>? Values;
    public int Guard = 123456;
}
