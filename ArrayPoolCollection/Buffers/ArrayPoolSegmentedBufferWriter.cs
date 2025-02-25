using System.Buffers;
using System.Runtime.CompilerServices;
using ArrayPoolCollection.Pool;

namespace ArrayPoolCollection.Buffers
{
    public sealed class ArrayPoolSegmentedBufferWriter<T> : IBufferWriter<T>, IDisposable
    {
        private T[][]? m_Arrays;
        private int[]? m_SegmentLength;
        private int m_SegmentIndex;
        private int m_Length;

        public int Capacity
        {
            get
            {
                if (m_Arrays is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
                }
                if (m_SegmentLength is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
                }

                return m_Length + m_Arrays[m_SegmentIndex].Length - m_SegmentLength[m_SegmentIndex];
            }
        }

        public int FreeCapacity
        {
            get
            {
                if (m_Arrays is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
                }
                if (m_SegmentLength is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
                }

                return m_Arrays[m_SegmentIndex].Length - m_SegmentLength[m_SegmentIndex];
            }
        }

        public int WrittenCount
        {
            get
            {
                if (m_Arrays is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
                }
                if (m_SegmentLength is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
                }

                return m_Length;
            }
        }


        public ArrayPoolSegmentedBufferWriter() : this(1024) { }
        public ArrayPoolSegmentedBufferWriter(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, CollectionHelper.ArrayMaxLength, capacity);
            }

            m_Arrays = SlimArrayPool<T[]>.Shared.Rent(32);
            m_Arrays.AsSpan().Clear();
            m_Arrays[0] = SlimArrayPool<T>.Shared.Rent(CollectionHelper.GetInitialPoolingSize(capacity));

            m_SegmentLength = SlimArrayPool<int>.Shared.Rent(32);
            m_SegmentLength.AsSpan().Clear();

            m_Length = 0;
            m_SegmentIndex = 0;
        }


        public void Advance(int count)
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }
            if (m_SegmentLength is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
            }
            if ((uint)count > (uint)(m_Arrays[m_SegmentIndex].Length - m_SegmentLength[m_SegmentIndex]))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Arrays[m_SegmentIndex].Length - m_SegmentLength[m_SegmentIndex], count);
            }

            m_Length += count;
            m_SegmentLength[m_SegmentIndex] += count;
        }

        public void Clear()
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }

            for (int i = 0; i < m_Arrays.Length; i++)
            {
                m_Arrays[i]?.AsSpan().Clear();
            }

            m_SegmentLength.AsSpan().Clear();

            m_SegmentIndex = 0;
            m_Length = 0;
        }

        public void Dispose()
        {
            if (m_Arrays is not null)
            {
                for (int i = 0; i < m_Arrays.Length; i++)
                {
                    if (m_Arrays[i] is not null)
                    {
                        SlimArrayPool<T>.Shared.Return(m_Arrays[i], RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                    }
                }
                SlimArrayPool<T[]>.Shared.Return(m_Arrays, true);
                m_Arrays = null;
            }
            if (m_SegmentLength is not null)
            {
                SlimArrayPool<int>.Shared.Return(m_SegmentLength);
                m_SegmentLength = null;
            }
        }

        public Memory<T> GetMemory(int sizeHint = 0)
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }
            if (m_SegmentLength is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
            }
            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(sizeHint), 0, CollectionHelper.ArrayMaxLength, sizeHint);
            }

            sizeHint = Math.Max(sizeHint, 1);
            ResizeIfOver(sizeHint);

            return m_Arrays[m_SegmentIndex].AsMemory(m_SegmentLength[m_SegmentIndex]..);
        }

        public Span<T> GetSpan(int sizeHint = 0)
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }
            if (m_SegmentLength is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
            }
            if (sizeHint < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(sizeHint), 0, CollectionHelper.ArrayMaxLength, sizeHint);
            }

            sizeHint = Math.Max(sizeHint, 1);
            ResizeIfOver(sizeHint);

            return m_Arrays[m_SegmentIndex].AsSpan(m_SegmentLength[m_SegmentIndex]..);
        }

        private void ResizeIfOver(int additionalSize)
        {
            if (m_SegmentLength![m_SegmentIndex] + additionalSize <= m_Arrays![m_SegmentIndex].Length)
            {
                return;
            }

            m_SegmentIndex++;
            if (m_Arrays!.Length >= m_SegmentIndex)
            {
                var oldArrays = m_Arrays;
                m_Arrays = SlimArrayPool<T[]>.Shared.Rent(oldArrays.Length << 1);
                oldArrays.AsSpan().CopyTo(m_Arrays);
                SlimArrayPool<T[]>.Shared.Return(oldArrays, true);

                var oldSegmentedLength = m_SegmentLength;
                m_SegmentLength = SlimArrayPool<int>.Shared.Rent(oldSegmentedLength!.Length << 1);
                oldSegmentedLength.AsSpan().CopyTo(m_SegmentLength);
                SlimArrayPool<int>.Shared.Return(oldSegmentedLength);
            }

            int nextLength = m_Arrays[m_SegmentIndex - 1].Length << 1;
            if (nextLength < 0)
            {
                nextLength = 1 << 30;
            }

            if (m_Arrays[m_SegmentIndex] is null)
            {
                m_Arrays[m_SegmentIndex] = SlimArrayPool<T>.Shared.Rent(nextLength);
            }
            else if (m_Arrays[m_SegmentIndex].Length < nextLength)
            {
                SlimArrayPool<T>.Shared.Return(m_Arrays[m_SegmentIndex], RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Arrays[m_SegmentIndex] = SlimArrayPool<T>.Shared.Rent(nextLength);
            }

            m_SegmentLength![m_SegmentIndex] = 0;
        }

        public ReadOnlySequence<T> GetWrittenSequence()
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }
            if (m_SegmentLength is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_SegmentLength));
            }

            int length = m_Length - m_SegmentLength[m_SegmentIndex];
            var last = new SequenceSegment(m_Arrays[m_SegmentIndex].AsMemory(..m_SegmentLength[m_SegmentIndex]), null, length);
            var next = last;

            for (int i = m_SegmentIndex - 1; i >= 0; i--)
            {
                length -= m_SegmentLength[i];
                var segment = new SequenceSegment(m_Arrays[i].AsMemory(..m_SegmentLength[i]), next, length);
                next = segment;
            }

            return new ReadOnlySequence<T>(next, 0, last, last.Memory.Length);
        }

        public void ResetWrittenCount()
        {
            if (m_Arrays is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Arrays));
            }

            m_SegmentLength.AsSpan().Clear();
            m_SegmentIndex = 0;
            m_Length = 0;
        }

        public sealed class SequenceSegment : ReadOnlySequenceSegment<T>
        {
            internal SequenceSegment(ReadOnlyMemory<T> memory, ReadOnlySequenceSegment<T>? next, int runningIndex)
            {
                Memory = memory;
                Next = next;
                RunningIndex = runningIndex;
            }
        }
    }
}
