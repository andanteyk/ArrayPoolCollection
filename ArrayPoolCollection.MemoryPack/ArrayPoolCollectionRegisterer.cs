using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public static class ArrayPoolCollectionRegisterer
    {
        public static void Register()
        {
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolWrapper<>), typeof(ArrayPoolWrapperFormatter<>));
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolList<>), typeof(ArrayPoolListFormatter<>));
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolDictionary<,>), typeof(ArrayPoolDictionaryFormatter<,>));
        }
    }
}
