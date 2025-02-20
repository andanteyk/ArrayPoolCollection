using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

public class ArrayPoolDictionaryFormatterTests
{
    [Fact]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolDictionary<int, int>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes));

        var source = new ArrayPoolDictionary<int, int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source[rng.Next()] = rng.Next();

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolDictionary<string, string>>(null);
        Assert.Null(MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes));

        var source = new ArrayPoolDictionary<string, string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes);
        Assert.Equal(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source[rng.NextDouble().ToString()] = rng.NextDouble().ToString();

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes);

            Assert.Equal(source, dest);
        }
    }

    [Fact]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new DictionaryWrapper<int, int>();

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<DictionaryWrapper<int, int>>(bytes)!;

        Assert.Null(dest.Values);
        Assert.Equal(source.Guard, dest.Guard);


        source.Values = new();

        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.Next(), rng.Next());
        }

        bytes = MemoryPackSerializer.Serialize(source);
        dest = MemoryPackSerializer.Deserialize<DictionaryWrapper<int, int>>(bytes)!;

        Assert.Equal(source.Values, dest.Values);
        Assert.Equal(source.Guard, dest.Guard);
    }

    [Fact]
    public void Overwrite()
    {
        var source = new ArrayPoolDictionary<string, int>(StringComparer.OrdinalIgnoreCase) { { "Alice", 16 } };

        var bytes = MemoryPackSerializer.Serialize(source);

        var dest = new ArrayPoolDictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        MemoryPackSerializer.Deserialize(bytes, ref dest!);

        Assert.Equal(StringComparer.OrdinalIgnoreCase, dest.Comparer);
        Assert.Equal(16, dest["alice"]);
    }
}

[MemoryPackable]
public partial class DictionaryWrapper<TKey, TValue>
    where TKey : notnull
{
    public ArrayPoolDictionary<TKey, TValue>? Values;
    public int Guard = 123456;
}
