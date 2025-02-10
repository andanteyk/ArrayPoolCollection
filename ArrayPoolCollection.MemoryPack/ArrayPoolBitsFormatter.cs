using MemoryPack;

namespace ArrayPoolCollection.MemoryPack
{
    public class ArrayPoolBitsFormatter : MemoryPackFormatter<ArrayPoolBits>
    {
        public override void Deserialize(ref MemoryPackReader reader, scoped ref ArrayPoolBits? value)
        {
            if (!reader.DangerousTryReadCollectionHeader(out int length))
            {
                value = null;
                return;
            }

            if (value is null)
            {
                value = new ArrayPoolBits(length);
            }
            else
            {
                value.Clear();
            }

            value.EnsureCapacity(length);
            ArrayPoolBits.SetCount(value, length);

            if (length > 0)
            {
                var span = ArrayPoolBits.AsSpan(value);
                int internalLength = (length + UIntPtr.Size * 8 - 1) >> (UIntPtr.Size == 4 ? 5 : 6);
                reader.ReadSpanWithoutReadLengthHeader(internalLength, ref span!);
            }
        }

        public override void Serialize<TBufferWriter>(ref MemoryPackWriter<TBufferWriter> writer, scoped ref ArrayPoolBits? value)
        {
            if (value is null)
            {
                writer.WriteNullCollectionHeader();
                return;
            }

            writer.WriteCollectionHeader(value.Count);
            var span = ArrayPoolBits.AsSpan(value);
            writer.WriteSpanWithoutLengthHeader<nuint>(span);
        }
    }
}
