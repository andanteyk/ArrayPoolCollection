using System.Buffers;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection.Buffers
{
    public sealed class ArrayPoolBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private T[]? m_Array;
        private int m_Length;

        public int Capacity
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Array.Length;
            }
        }

        public int FreeCapacity
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Array.Length - m_Length;
            }
        }

        public int WrittenCount
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Length;
            }
        }

        public ReadOnlyMemory<T> WrittenMemory
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Array.AsMemory(..m_Length);
            }
        }

        public ReadOnlySpan<T> WrittenSpan
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Array.AsSpan(..m_Length);
            }
        }


        public ArrayPoolBufferWriter() : this(1024) { }
        public ArrayPoolBufferWriter(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, CollectionHelper.ArrayMaxLength, capacity);
            }

            m_Array = ArrayPool<T>.Shared.Rent(capacity);
            m_Length = 0;
        }

        public void Advance(int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Array.Length - m_Length, count);
            }
            if (m_Length + count > m_Array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Array.Length - m_Length, count);
            }

            m_Length += count;
        }

        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Array.AsSpan(..m_Length).Clear();
            m_Length = 0;
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Array = null;
            }
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(sizeHint), 0, CollectionHelper.ArrayMaxLength, sizeHint);
            }

            sizeHint = Math.Max(sizeHint, 1);

            int nextLength = m_Length + sizeHint;
            ResizeIfOver(nextLength);
            return m_Array.AsMemory(m_Length..);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(sizeHint), 0, CollectionHelper.ArrayMaxLength, sizeHint);
            }

            sizeHint = Math.Max(sizeHint, 1);

            int nextLength = m_Length + sizeHint;
            ResizeIfOver(nextLength);
            return m_Array.AsSpan(m_Length..);
        }

        private void ResizeIfOver(int newSize)
        {
            if (newSize > m_Array!.Length)
            {
                var oldArray = m_Array;
                m_Array = ArrayPool<T>.Shared.Rent(CollectionHelper.RoundUpToPowerOf2(newSize));
                oldArray.AsSpan(..m_Length).CopyTo(m_Array);
                ArrayPool<T>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }
        }

        public void ResetWrittenCount()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Length = 0;
        }
    }
}
