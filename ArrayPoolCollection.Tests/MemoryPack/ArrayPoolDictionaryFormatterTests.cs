using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolDictionaryFormatterTests
{
    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolDictionary<int, int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes));

        var source = new ArrayPoolDictionary<int, int>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source[rng.Next()] = rng.Next();

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<int, int>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolDictionary<string, string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes));

        var source = new ArrayPoolDictionary<string, string>();
        bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes);
        CollectionAssert.AreEqual(source, dest);

        for (int i = 0; i < 1024; i++)
        {
            source[rng.NextDouble().ToString()] = rng.NextDouble().ToString();

            bytes = MemoryPackSerializer.Serialize(source);
            dest = MemoryPackSerializer.Deserialize<ArrayPoolDictionary<string, string>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new DictionaryWrapper<int, int>();
        for (int i = 0; i < 16; i++)
        {
            source.Values.Add(rng.Next(), rng.Next());
        }

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<DictionaryWrapper<int, int>>(bytes)!;

        CollectionAssert.AreEqual(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class DictionaryWrapper<TKey, TValue>
    where TKey : notnull
{
    public ArrayPoolDictionary<TKey, TValue> Values = new();
}
