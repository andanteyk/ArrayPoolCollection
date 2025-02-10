using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolStackFormatter<T> : MemoryPackFormatter<ArrayPoolStack<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolStack<T>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            if (value is null)
            {
                value = new ArrayPoolStack<T>(length);
            }
            else
            {
                value.Clear();
            }

            value.EnsureCapacity(length);

            if (length > 0)
            {
                ArrayPoolStack<T>.SetCount(value, length);
                var span = ArrayPoolStack<T>.AsSpan(value);
                reader.ReadSpan(ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolStack<T>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = ArrayPoolStack<T>.AsSpan(value);
            writer.WriteSpan(span!);
        }
    }
}
