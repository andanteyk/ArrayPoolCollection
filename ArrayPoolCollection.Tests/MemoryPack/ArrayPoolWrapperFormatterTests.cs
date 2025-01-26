using System.Runtime.InteropServices;
using MemoryPack;

namespace ArrayPoolCollection.MemoryPack.Tests;

[TestClass]
public class ArrayPoolWrapperFormatterTests
{
    [AssemblyInitialize]
    public static void GlobalSetup(TestContext context)
    {
        ArrayPoolCollectionRegisterer.Register();
    }

    [TestMethod]
    public void SerializeInt()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolWrapper<int>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolWrapper<int>>(bytes));

        for (int i = 0; i <= 1024; i++)
        {
            var source = new ArrayPoolWrapper<int>(i);
            rng.NextBytes(MemoryMarshal.AsBytes(source.AsSpan()));

            bytes = MemoryPackSerializer.Serialize(source);
            var dest = MemoryPackSerializer.Deserialize<ArrayPoolWrapper<int>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeString()
    {
        var rng = new Random(0);

        var bytes = MemoryPackSerializer.Serialize<ArrayPoolWrapper<string>>(null);
        Assert.IsNull(MemoryPackSerializer.Deserialize<ArrayPoolWrapper<string>>(bytes));

        for (int i = 0; i <= 1024; i++)
        {
            var source = new ArrayPoolWrapper<string>(i);
            for (int k = 0; k < i; k++)
            {
                source[k] = rng.NextDouble().ToString();
            }

            bytes = MemoryPackSerializer.Serialize(source);
            var dest = MemoryPackSerializer.Deserialize<ArrayPoolWrapper<string>>(bytes);

            CollectionAssert.AreEqual(source, dest);
        }
    }

    [TestMethod]
    public void SerializeWrappedClass()
    {
        var rng = new Random(0);
        var source = new ArrayWrapper<int>();
        rng.NextBytes(MemoryMarshal.AsBytes(source.Values.AsSpan()));

        var bytes = MemoryPackSerializer.Serialize(source);
        var dest = MemoryPackSerializer.Deserialize<ArrayWrapper<int>>(bytes)!;

        CollectionAssert.AreEqual(source.Values, dest.Values);
    }
}

[MemoryPackable]
public partial class ArrayWrapper<T>
{
    public ArrayPoolWrapper<T> Values = new(16);
}
