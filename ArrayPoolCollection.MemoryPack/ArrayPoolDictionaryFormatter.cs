using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolDictionaryFormatter<TKey, TValue> : MemoryPackFormatter<ArrayPoolDictionary<TKey, TValue>>
        where TKey : notnull
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolDictionary<TKey, TValue>? value)
        {
            if (!reader.TryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            value = new ArrayPoolDictionary<TKey, TValue>(length);
            var pair = new KeyValuePair<TKey, TValue>();
            for (int i = 0; i < length; i++)
            {
                reader.ReadValue(ref pair);
                value.Add(pair);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolDictionary<TKey, TValue>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = ArrayPoolDictionary<TKey, TValue>.AsSpan(value);
            writer.WriteSpan(span);
        }
    }
}
