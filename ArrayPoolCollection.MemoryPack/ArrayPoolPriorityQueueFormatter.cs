using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolPriorityQueueFormatter<TElement, TPriority> : MemoryPackFormatter<ArrayPoolPriorityQueue<TElement, TPriority>>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolPriorityQueue<TElement, TPriority>? value)
        {
            if (!reader.TryPeekCollectionHeader(out int length))
            {
                reader.Advance(4);
                value = null;
                return;
            }

            if (value is null)
            {
                value = new ArrayPoolPriorityQueue<TElement, TPriority>(length);
            }
            else
            {
                value.Clear();
            }

            value.EnsureCapacity(length);

            if (length > 0)
            {
                ArrayPoolPriorityQueue<TElement, TPriority>.SetCount(value, length);
                var span = ArrayPoolPriorityQueue<TElement, TPriority>.AsSpan(value);
                reader.ReadSpan(ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolPriorityQueue<TElement, TPriority>? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            var span = ArrayPoolPriorityQueue<TElement, TPriority>.AsSpan(value);
            writer.WriteSpan(span!);
        }
    }
}
