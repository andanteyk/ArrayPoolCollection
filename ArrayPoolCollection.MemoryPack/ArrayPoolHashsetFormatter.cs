using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolHashSetFormatter<T> : MemoryPackFormatter<ArrayPoolHashSet<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolHashSet<T>? value)
        {
            if (!reader.TryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            value = new ArrayPoolHashSet<T>(length);
            T? key = default;
            for (int i = 0; i < length; i++)
            {
                reader.ReadValue(ref key);
                value.Add(key!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolHashSet<T>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = ArrayPoolHashSet<T>.AsSpan(value);
            writer.WriteSpan(span!);
        }
    }
}
