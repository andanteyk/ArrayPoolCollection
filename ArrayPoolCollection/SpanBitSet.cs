using System.Buffers;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection
{
    internal ref struct SpanBitSet
    {
        private nuint[]? m_Array;
        private Span<nuint> m_Span;
        private ReadOnlySpan<int> m_VersionRef;
        private int m_Length;
        private int m_Version;


        public readonly static int StackallocThreshold = 64;
        private readonly static int NuintBits = UIntPtr.Size == 4 ? 32 : 64;
        private readonly static int NuintShifts = UIntPtr.Size == 4 ? 5 : 6;
        private const nuint Zero = 0;
        private const nuint One = 1;


        private readonly bool IsDisposed => m_VersionRef[0] == -1;

        public bool this[int index]
        {
            readonly get
            {
                if (IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }

                return (m_Span[index >> NuintShifts] & (One << index)) != 0;
            }
            set
            {
                if (IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }

                if (value)
                {
                    m_Span[index >> NuintShifts] |= One << index;
                }
                else
                {
                    m_Span[index >> NuintShifts] &= ~(One << index);
                }

                AddVersion();
            }
        }

        public readonly int Count
        {
            get
            {
                if (IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }

                return m_Length;
            }
        }


        public SpanBitSet(Span<nuint> span) : this(span, 0) { }
        public SpanBitSet(Span<nuint> span, int initialLength)
        {
            if (initialLength > span.Length * NuintBits)
            {
                int arrayCapacity = (initialLength + NuintBits - 1) >> NuintShifts;
                m_Array = ArrayPool<nuint>.Shared.Rent(arrayCapacity);
                span = m_Array;
            }

            m_Span = span;
            m_Span.Clear();
            m_Array = null;

            m_Length = initialLength;
            m_Version = 0;
            m_VersionRef = MemoryMarshal.CreateReadOnlySpan(ref m_Version, 1);
        }

        public SpanBitSet(int capacityBits) : this(capacityBits, 0) { }
        public SpanBitSet(int capacityBits, int initialLength)
        {
            if (capacityBits < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacityBits), 0, int.MaxValue, capacityBits);
            }
            if (initialLength > capacityBits * NuintBits)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(initialLength), 0, m_Span.Length * NuintBits, initialLength);
            }

            int arrayCapacity = (capacityBits + NuintBits - 1) >> NuintShifts;
            m_Array = ArrayPool<nuint>.Shared.Rent(arrayCapacity);

            m_Span = m_Array;
            m_Span.Clear();

            m_Length = initialLength;
            m_Version = 0;
            m_VersionRef = MemoryMarshal.CreateReadOnlySpan(ref m_Version, 1);
        }


        private void AddVersion()
        {
            m_Version = (m_Version + 1) & int.MaxValue;
        }

        public void Add(bool item)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length >= m_Span.Length * NuintBits)
            {
                Resize(m_Length << 1);
            }

            m_Length++;
            this[m_Length - 1] = item;
            AddVersion();
        }

        private void Resize(int size)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var oldArray = m_Array;
            m_Array = ArrayPool<nuint>.Shared.Rent(size >> NuintShifts);
            m_Span.CopyTo(m_Array);

            if (oldArray is not null)
            {
                ArrayPool<nuint>.Shared.Return(oldArray);
            }

            m_Span = m_Array;
            AddVersion();
        }

        public void Clear()
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Length = 0;
            AddVersion();
        }

        public readonly bool Contains(bool item)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            nuint mask = item ? Zero : ~Zero;
            int limit = m_Length >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                if (m_Span[i] != mask)
                {
                    return true;
                }
            }

            if ((m_Length & (NuintBits - 1)) != 0)
            {
                mask >>= NuintBits - (m_Length & (NuintBits - 1));
                return m_Span[(m_Length >> NuintShifts) + 1] != mask;
            }

            return false;
        }

        public readonly void CopyTo(bool[] array, int arrayIndex)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (arrayIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(arrayIndex), 0, array.Length, arrayIndex);
            }
            if (array.Length - arrayIndex < m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, m_Length - arrayIndex, arrayIndex);
            }

            for (int i = 0; i < m_Length; i++)
            {
                array[i + arrayIndex] = this[i];
            }
        }

        public readonly void CopyTo(Span<bool> span)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            for (int i = 0; i < m_Length; i++)
            {
                span[i] = this[i];
            }
        }

        public void Dispose()
        {
            if (m_Array != null)
            {
                ArrayPool<nuint>.Shared.Return(m_Array);
                m_Array = null;
            }
            m_Length = 0;
            m_Version = -1;
        }

        public readonly bool Get(int index)
        {
            return this[index];
        }

        public ref struct Enumerator
        {
            private readonly SpanBitSet m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(SpanBitSet parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            public readonly bool Current
            {
                get
                {
                    if (m_Parent.IsDisposed)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                    }
                    if (m_Version != m_Parent.m_VersionRef[0])
                    {
                        ThrowHelper.ThrowDifferentVersion();
                    }
                    if ((uint)m_Index >= m_Parent.m_Length)
                    {
                        ThrowHelper.ThrowEnumeratorUndefined();
                    }

                    return m_Parent[m_Index];
                }
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Parent.IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Version != m_Parent.m_VersionRef[0])
                {
                    ThrowHelper.ThrowDifferentVersion();
                }
                if (m_Index >= m_Parent.m_Length)
                {
                    return false;
                }

                return ++m_Index < m_Parent.m_Length;
            }

            public void Reset()
            {
                if (m_Parent.IsDisposed)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Version != m_Parent.m_VersionRef[0])
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                m_Index = -1;
            }
        }

        public readonly Enumerator GetEnumerator()
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return new Enumerator(this);
        }

        public readonly int IndexOf(bool item)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            nuint mask = item ? Zero : ~Zero;
            int limit = m_Length >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                var masked = m_Span[i] ^ mask;
                if (masked != 0)
                {
                    return i * NuintBits + CollectionHelper.TrailingZeroCount(masked);
                }
            }

            if ((m_Length & (NuintBits - 1)) != 0)
            {
                mask &= (One << m_Length) - 1;
                var masked = m_Span[limit] ^ mask;
                if (masked != 0)
                {
                    return limit * NuintBits + CollectionHelper.TrailingZeroCount(masked);
                }
            }

            return -1;
        }

        public void Insert(int index, bool item)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, index);
            }
            if (index > m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Length + 1, index);
            }

            if (m_Length >= m_Span.Length * NuintBits)
            {
                Resize(m_Length << 1);
            }

            int startIndex = index >> NuintShifts;
            int startOffset = index & (NuintBits - 1);
            nuint startMask = (One << startOffset) - 1;

            nuint carry = m_Span[startIndex] >> (NuintBits - 1);
            m_Span[startIndex] = (m_Span[startIndex] & startMask) ^ ((item ? One : Zero) << startOffset) ^ (m_Span[startIndex] & ~startMask) << 1;

            int limit = m_Length >> NuintShifts;
            for (int i = startIndex + 1; i <= limit; i++)
            {
                var currentCarry = m_Span[i] >> (NuintBits - 1);
                m_Span[i] = m_Span[i] << 1 ^ carry;
                carry = currentCarry;
            }

            m_Length++;
            AddVersion();
        }

        public bool Remove(bool item)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AddVersion();

            nuint mask = item ? Zero : ~Zero;
            int limit = (m_Length + NuintBits - 1) >> NuintShifts;
            for (int i = 0; i < limit; i++)
            {
                nuint masked = m_Span[i] ^ mask;
                if (masked != 0)
                {
                    int offset = CollectionHelper.TrailingZeroCount(masked);
                    RemoveAt(i * NuintBits + offset);
                    return true;
                }
            }
            return false;
        }

        public void RemoveAt(int index)
        {
            if (IsDisposed)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, index);
            }
            if (index >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Length, index);
            }

            int startIndex = index >> NuintShifts;
            int startOffset = index & (NuintBits - 1);
            nuint startMask = (One << startOffset) - 1;

            m_Span[startIndex] = (m_Span[startIndex] & startMask) ^ ((m_Span[startIndex] >> 1) & ~startMask);

            int limit = m_Length >> NuintShifts;
            for (int i = startIndex + 1; i <= limit; i++)
            {
                m_Span[i - 1] ^= m_Span[i] & 1;
                m_Span[i] = m_Span[i] >> 1;
            }

            m_Length--;
            AddVersion();
        }

        public void Set(int index, bool value)
        {
            this[index] = value;
        }

        public override string ToString()
        {
            return $"{Count} items";
        }
    }
}
