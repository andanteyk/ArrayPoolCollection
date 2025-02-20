using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolQueueFormatter<T> : MemoryPackFormatter<ArrayPoolQueue<T>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolQueue<T>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                reader.Advance(4);
                value = null;
                return;
            }

            if (value is null)
            {
                value = new ArrayPoolQueue<T>(length);
            }
            else
            {
                value.Clear();
            }

            value.EnsureCapacity(length);

            if (length > 0)
            {
                ArrayPoolQueue<T>.SetCount(value, length);
                ArrayPoolQueue<T>.AsSpan(value, out var span, out _);
                reader.ReadSpan(ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolQueue<T>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            ArrayPoolQueue<T>.AsSpan(value, out var headSide, out var tailSide);
            writer.WriteSpan(headSide!);
            writer.WriteSpanWithoutLengthHeader<T>(tailSide!);
        }
    }
}
