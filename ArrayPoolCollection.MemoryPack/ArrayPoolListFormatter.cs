using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolListFormatter<T> : MemoryPackFormatter<ArrayPoolList<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolList<T>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            value = new ArrayPoolList<T>(length);
            if (length > 0)
            {
                value.SetCount(length);
                var span = value.AsSpan();
                reader.ReadSpan(ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolList<T>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = value.AsSpan();
            writer.WriteSpan(span!);
        }
    }
}
