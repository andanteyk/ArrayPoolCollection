using System.Buffers;
using System.Collections;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolBits : IList<bool>, IReadOnlyList<bool>, IList, IDisposable
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

        bool IList.IsFixedSize => throw new NotImplementedException();

        bool ICollection.IsSynchronized => throw new NotImplementedException();

        object ICollection.SyncRoot => throw new NotImplementedException();

        object IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ArrayPoolBits() : this(0) { }
        public ArrayPoolBits(int bitLength)
        {
            if (bitLength < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(bitLength), 0, int.MaxValue, bitLength);
            }

            int arrayLength = (bitLength + NuintBits - 1) >> NuintShifts;
            m_Array = ArrayPool<nuint>.Shared.Rent(Math.Max(arrayLength, 16));
            m_Length = bitLength;

            m_Array.AsSpan(..arrayLength).Clear();
        }

        public ArrayPoolBits(IEnumerable<bool> bools)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(bools, out int count))
            {
                count = NuintBits;
            }

            int arrayLength = (count + NuintBits - 1) >> NuintShifts;
            m_Array = ArrayPool<nuint>.Shared.Rent(arrayLength);

            foreach (var bit in bools)
            {
                Add(bit);
            }
        }
        public ArrayPoolBits(ReadOnlySpan<bool> bools)
        {
            int arrayLength = (bools.Length + NuintBits - 1) >> NuintShifts;
            m_Array = ArrayPool<nuint>.Shared.Rent(arrayLength);
            m_Length = bools.Length;

            for (int i = 0; i < bools.Length; i++)
            {
                this[i] = bools[i];
            }
        }
        public ArrayPoolBits(bool[] bools) : this(bools.AsSpan()) { }
        public ArrayPoolBits(ReadOnlySpan<byte> bytes)
        {
            int arrayLength = (bytes.Length * 8 + NuintBits - 1) >> NuintShifts;
            m_Array = ArrayPool<nuint>.Shared.Rent(arrayLength);

            bytes.CopyTo(MemoryMarshal.AsBytes(m_Array.AsSpan()));
            m_Length = bytes.Length * 8;
        }
        public ArrayPoolBits(ArrayPoolBits source)
        {
            if (source.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Array = ArrayPool<nuint>.Shared.Rent(source.m_Array.Length);
            source.m_Array.AsSpan().CopyTo(m_Array);
            m_Length = source.m_Length;
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

        public void And(ArrayPoolBits bits)
        {
            if (m_Array is null || bits.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length != bits.m_Length)
            {
                ThrowHelper.ThrowLengthIsDifferent();
            }

            for (int i = (m_Length - 1) >> NuintShifts; i >= 0; i--)
            {
                m_Array[i] &= bits.m_Array[i];
            }

            m_Version++;
        }

        /// <summary>
        /// `AsSpan()` works similarly to `CollectionsMarshal.AsSpan()`.
        /// Note that adding or removing elements from a collection may reference discarded buffers.
        /// </summary>
        public static Span<nuint> AsSpan(ArrayPoolBits bits)
        {
            if (bits.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return bits.m_Array.AsSpan(..((bits.m_Length + NuintBits - 1) >> NuintShifts));
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
            int limit = (m_Length - 1) >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                if (m_Array[i] != mask)
                {
                    return true;
                }
            }

            int rest = m_Length & (NuintBits - 1);
            if (rest != 0)
            {
                mask >>= NuintBits - rest;
                return (m_Array[limit] & (~Zero >> (NuintBits - rest))) != mask;
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

            // TODO: opt.
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
            private readonly ArrayPoolBits m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(ArrayPoolBits parent)
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
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return new Enumerator(this);
        }

        IEnumerator<bool> IEnumerable<bool>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool HasAllSet()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int lastIndex = m_Length >> NuintShifts;
            if ((m_Length & (NuintBits - 1)) != 0)
            {
                nuint lastMask = ~Zero >> (NuintBits - m_Length);

                if ((m_Array[lastIndex] & lastMask) != lastMask)
                {
                    return false;
                }
            }

            for (int i = lastIndex - 1; i >= 0; i--)
            {
                if (m_Array[i] != ~Zero)
                {
                    return false;
                }
            }

            return true;
        }

        public bool HasAnySet()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int lastIndex = m_Length >> NuintShifts;
            if ((m_Length & (NuintBits - 1)) != 0)
            {
                nuint lastMask = ~Zero >> (NuintBits - m_Length);

                if ((m_Array[lastIndex] & lastMask) != 0)
                {
                    return true;
                }
            }

            for (int i = lastIndex - 1; i >= 0; i--)
            {
                if (m_Array[i] != Zero)
                {
                    return true;
                }
            }

            return false;
        }

        public int IndexOf(bool item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            nuint mask = item ? Zero : ~Zero;
            int limit = (m_Length - 1) >> NuintShifts;

            for (int i = 0; i < limit; i++)
            {
                var masked = m_Array[i] ^ mask;
                if (masked != 0)
                {
                    return i * NuintBits + CollectionHelper.TrailingZeroCount(masked);
                }
            }

            int rest = m_Length & (NuintBits - 1);
            if (rest != 0)
            {
                mask >>= NuintBits - rest;
                var masked = (m_Array[limit] & (~Zero >> (NuintBits - rest))) ^ mask;
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

            m_Length++;
            m_Version++;
        }

        public void LeftShift(int shift)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (shift < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(shift), 0, int.MaxValue, shift);
            }

            if (shift >= m_Length)
            {
                SetAll(false);
                return;
            }

            int localShift = shift & (NuintBits - 1);
            int globalShift = shift >> NuintShifts;
            int limit = (m_Length - 1) >> NuintShifts;

            for (int i = limit; i >= 0; i--)
            {
                int globalIndex = i - globalShift;
                m_Array[i] =
                    (globalIndex >= 0 ? m_Array[globalIndex] << localShift : 0) ^
                    (globalIndex - 1 >= 0 ? m_Array[globalIndex - 1] >> -localShift : 0);
            }

            m_Version++;
        }

        public void Not()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int lastIndex = (m_Length - 1) >> NuintShifts;
            nuint lastMask = ~Zero >> (NuintBits - m_Length);

            m_Array[lastIndex] ^= lastMask;

            for (int i = lastIndex - 1; i >= 0; i--)
            {
                m_Array[i] ^= ~Zero;
            }

            m_Version++;
        }

        public void Or(ArrayPoolBits bits)
        {
            if (m_Array is null || bits.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length != bits.m_Length)
            {
                ThrowHelper.ThrowLengthIsDifferent();
            }

            for (int i = (m_Length - 1) >> NuintShifts; i >= 0; i--)
            {
                m_Array[i] |= bits.m_Array[i];
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

        public void RightShift(int shift)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (shift < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(shift), 0, int.MaxValue, shift);
            }

            if (shift >= m_Length)
            {
                SetAll(false);
                return;
            }

            int localShift = shift & (NuintBits - 1);
            int globalShift = shift >> NuintShifts;
            int limit = (m_Length - 1) >> NuintShifts;

            for (int i = 0; i <= limit; i++)
            {
                int globalIndex = i + globalShift;

                if (globalIndex > limit)
                {
                    m_Array[i] = Zero;
                }
                else if (globalIndex == limit)
                {
                    var mask = ~Zero >> (NuintBits - (m_Length & (NuintBits - 1)));
                    m_Array[i] =
                        (m_Array[globalIndex] & mask) >> localShift;
                }
                else if (globalIndex + 1 == limit)
                {
                    var mask = ~Zero >> (NuintBits - (m_Length & (NuintBits - 1)));
                    m_Array[i] =
                        m_Array[globalIndex] >> localShift ^
                        (m_Array[globalIndex + 1] & mask) << -localShift;
                }
                else
                {
                    m_Array[i] =
                        m_Array[globalIndex] >> localShift ^
                        m_Array[globalIndex + 1] << -localShift;
                }
            }

            m_Version++;
        }

        public void SetAll(bool value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int lastIndex = (m_Length - 1) >> NuintShifts;
            nuint lastMask = ~Zero >> (NuintBits - m_Length);

            m_Array[lastIndex] = value ? lastMask : Zero;

            for (int i = lastIndex - 1; i >= 0; i--)
            {
                m_Array[i] = value ? ~Zero : Zero;
            }

            m_Version++;
        }

        /// <summary>
        /// `SetCount()` works similarly as `CollectionsMarshal.SetCount()`.
        /// Use with caution as it may reference uninitialized area.
        /// </summary>
        public static void SetCount(ArrayPoolBits source, int count)
        {
            if (source.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, source.m_Array.Length * NuintBits, count);
            }
            if (count > source.m_Array.Length * NuintBits)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, source.m_Array.Length * NuintBits, count);
            }

            source.m_Length = count;
            source.m_Version++;
        }

        public void Xor(ArrayPoolBits bits)
        {
            if (m_Array is null || bits.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length != bits.m_Length)
            {
                ThrowHelper.ThrowLengthIsDifferent();
            }

            for (int i = (m_Length - 1) >> NuintShifts; i >= 0; i--)
            {
                m_Array[i] ^= bits.m_Array[i];
            }

            m_Version++;
        }

        public override string ToString()
        {
            return $"{Count} items";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        int IList.Add(object value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (value is bool bit)
            {
                Add(bit);
                return m_Length - 1;
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            return -1;
        }

        bool IList.Contains(object value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (value is bool bit)
            {
                return Contains(bit);
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            return default;
        }

        int IList.IndexOf(object value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (value is bool bit)
            {
                return IndexOf(bit);
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (value is bool bit)
            {
                Insert(index, bit);
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
        }

        void IList.Remove(object value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (value is bool bit)
            {
                Remove(bit);
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (array.Rank > 1)
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }
            if (array is bool[] boolArray)
            {
                CopyTo(boolArray, index);
            }
            else
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }
        }
    }
}
