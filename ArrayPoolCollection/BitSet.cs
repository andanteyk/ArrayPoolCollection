using System.Buffers;
using System.Collections;

namespace ArrayPoolCollection
{
    internal class BitSet : IList<bool>, IReadOnlyList<bool>, IDisposable
    {
        private nuint[]? m_Array;
        private int m_Length;
        private int m_Version;

        private static int NuintBits => UIntPtr.Size == 4 ? 32 : 64;
        private static int NuintShifts => UIntPtr.Size == 4 ? 5 : 6;
        private const nuint Zero = 0;
        private const nuint One = 1;


        public bool this[int index]
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }

                return (m_Array[index >> NuintShifts] & (One << (index & (NuintBits - 1)))) != 0;
            }
            set
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }

                if (value)
                {
                    m_Array[index >> NuintShifts] |= One << (index & (NuintBits - 1));
                }
                else
                {
                    m_Array[index >> NuintShifts] &= ~(One << (index & (NuintBits - 1)));
                }
                m_Version++;
            }
        }

        public int Count
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

        public bool IsReadOnly => false;


        public BitSet() : this(32) { }
        public BitSet(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            int length = CollectionHelper.RoundUpToPowerOf2(Math.Max(capacity, NuintBits));
            m_Array = ArrayPool<nuint>.Shared.Rent(length >> NuintShifts);
            m_Array.AsSpan().Clear();
        }



        public void Add(bool item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length >= m_Array.Length * NuintBits)
            {
                Resize(m_Length << 1);
            }

            m_Length++;
            this[m_Length - 1] = item;
            m_Version++;
        }

        private void Resize(int size)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var oldArray = m_Array;
            m_Array = ArrayPool<nuint>.Shared.Rent(size >> NuintShifts);
            oldArray.AsSpan().CopyTo(m_Array);
            ArrayPool<nuint>.Shared.Return(oldArray);

            m_Version++;
        }

        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Length = 0;
            m_Version++;
        }

        public bool Contains(bool item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            nuint mask = item ? Zero : ~Zero;
            int limit = m_Length >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                if (m_Array[i] != mask)
                {
                    return true;
                }
            }

            if ((m_Length & (NuintBits - 1)) != 0)
            {
                mask >>= NuintBits - (m_Length & (NuintBits - 1));
                return m_Array[(m_Length >> NuintShifts) + 1] != mask;
            }

            return false;
        }

        public void CopyTo(bool[] array, int arrayIndex)
        {
            if (m_Array is null)
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

        public void Dispose()
        {
            if (m_Array != null)
            {
                ArrayPool<nuint>.Shared.Return(m_Array);
                m_Array = null;
            }
            m_Length = 0;
            m_Version = int.MinValue;
        }

        public struct Enumerator : IEnumerator<bool>
        {
            private readonly BitSet m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(BitSet parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            public readonly bool Current
            {
                get
                {
                    if (m_Parent.m_Array is null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                    }
                    if (m_Version != m_Parent.m_Version)
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

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Parent.m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Version != m_Parent.m_Version)
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
                if (m_Parent.m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Version != m_Parent.m_Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                m_Index = -1;
            }
        }

        public Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(bool item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            nuint mask = item ? Zero : ~Zero;
            int limit = m_Length >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                var masked = m_Array[i] ^ mask;
                if (masked != 0)
                {
                    return i * NuintBits + CollectionHelper.TrailingZeroCount(masked);
                }
            }

            if ((m_Length & (NuintBits - 1)) != 0)
            {
                mask >>= NuintBits - (m_Length & (NuintBits - 1));
                var masked = m_Array[(m_Length >> NuintShifts) + 1] ^ mask;
                if (masked != 0)
                {
                    return limit * NuintBits + CollectionHelper.TrailingZeroCount(masked);
                }
            }

            return -1;
        }

        public void Insert(int index, bool item)
        {
            if (m_Array is null)
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

            if (m_Length >= m_Array.Length * NuintBits)
            {
                Resize(m_Length << 1);
            }

            int startIndex = index >> NuintShifts;
            int startOffset = index & (NuintBits - 1);
            nuint startMask = (One << startOffset) - 1;

            nuint carry = m_Array[startIndex] >> (NuintBits - 1);
            m_Array[startIndex] = (m_Array[startIndex] & startMask) ^ ((item ? One : Zero) << startOffset) ^ (m_Array[startIndex] & ~startMask) << 1;

            int limit = m_Length >> NuintShifts;
            for (int i = startIndex + 1; i <= limit; i++)
            {
                var currentCarry = m_Array[i] >> (NuintBits - 1);
                m_Array[i] = m_Array[i] << 1 ^ carry;
                carry = currentCarry;
            }

            m_Version++;
        }

        public bool Remove(bool item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Version++;

            nuint mask = item ? Zero : ~Zero;
            int limit = (m_Length + NuintBits - 1) >> NuintShifts;
            for (int i = 0; i < limit; i++)
            {
                nuint masked = m_Array[i] ^ mask;
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
            if (m_Array is null)
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

            m_Array[startIndex] = (m_Array[startIndex] & startMask) ^ ((m_Array[startIndex] >> 1) & ~startMask);

            int limit = m_Length >> NuintShifts;
            for (int i = startIndex + 1; i <= limit; i++)
            {
                m_Array[i - 1] ^= m_Array[i] & 1;
                m_Array[i] = m_Array[i] >> 1;
            }

            m_Length--;
            m_Version++;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
