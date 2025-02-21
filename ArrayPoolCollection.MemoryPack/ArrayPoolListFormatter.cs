using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolListFormatter<T> : MemoryPackFormatter<ArrayPoolList<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolList<T>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                reader.Advance(4);
                value = null;
                return;
            }

            if (value is null)
            {
                value = new ArrayPoolList<T>(length);
            }
            else
            {
                value.Clear();
            }

            value.EnsureCapacity(length);

            ArrayPoolList<T>.SetCount(value, length);
            var span = ArrayPoolList<T>.AsSpan(value);
            reader.ReadSpan(ref span!);
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolList<T>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = ArrayPoolList<T>.AsSpan(value);
            writer.WriteSpan(span!);
        }
    }
}
