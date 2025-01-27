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
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolHashSet<>), typeof(ArrayPoolHashSetFormatter<>));
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolStack<>), typeof(ArrayPoolStackFormatter<>));
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolQueue<>), typeof(ArrayPoolQueueFormatter<>));
            MemoryPackFormatterProvider.RegisterGenericType(typeof(ArrayPoolPriorityQueue<,>), typeof(ArrayPoolPriorityQueueFormatter<,>));
            MemoryPackFormatterProvider.Register(new ArrayPoolBitsFormatter());
        }
    }
}
