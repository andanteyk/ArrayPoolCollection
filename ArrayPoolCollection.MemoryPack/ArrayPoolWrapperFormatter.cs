using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolWrapperFormatter<T> : MemoryPackFormatter<ArrayPoolWrapper<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolWrapper<T>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            value = new ArrayPoolWrapper<T>(length);
            if (length > 0)
            {
                var span = value.AsSpan();
                reader.ReadSpan(ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolWrapper<T>? value)
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
