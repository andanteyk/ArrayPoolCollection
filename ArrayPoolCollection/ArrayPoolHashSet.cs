using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolHashSet<T> : ICollection<T>, IReadOnlyCollection<T>, ISet<T>, IReadOnlySet<T>, IDisposable
    {
        private readonly record struct Metadata(uint Fingerprint, int ValueIndex)
        {
            public override string ToString()
            {
                return $"dist={Fingerprint >> 8} fingerprint={(byte)Fingerprint:x2} value={ValueIndex}";
            }
        }

        private const int DistanceUnit = 0x100;
        private const long MaxLoadFactorNum = 25;
        private const long MaxLoadFactorDen = 32;


        private T[]? m_Values;
        private Metadata[]? m_Metadata;

        private int m_Size;
        private int m_Shifts;      // TODO: not needed?
        private int m_Version;

        private readonly IEqualityComparer<T>? m_Comparer;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HashCodeToFingerprint(int hashCode)
        {
            return DistanceUnit | ((uint)hashCode & 0xff);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HashCodeToMetadataIndex(int hashCode, int shift)
        {
            return (int)((uint)hashCode >> shift);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AreEqual(T key1, T key2, IEqualityComparer<T>? comparer)
        {
            if (typeof(T).IsValueType)
            {
                if (comparer == null)
                {
                    return EqualityComparer<T>.Default.Equals(key1, key2);
                }

                return comparer.Equals(key1, key2);
            }
            else
            {
                return comparer!.Equals(key1, key2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHashCode(T key, IEqualityComparer<T>? comparer)
        {
            if (typeof(T).IsValueType)
            {
                if (comparer == null)
                {
                    return key!.GetHashCode();
                }

                return comparer.GetHashCode(key!);
            }
            else
            {
                if (key is { } notnullKey)
                {
                    return comparer!.GetHashCode(notnullKey);
                }
                else
                {
                    return 0;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int IncrementMetadataIndex(int metadataIndex, int metadataLength)
        {
            return (metadataIndex + 1) & (metadataLength - 1);
        }


        private int GetEntryIndex(T key)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var metadata = m_Metadata;
            var values = m_Values;
            var comparer = m_Comparer;

            int hashCode = GetHashCode(key, comparer);
            uint fingerprint = HashCodeToFingerprint(hashCode);
            int metadataIndex = HashCodeToMetadataIndex(hashCode, m_Shifts);

            var current = metadata[metadataIndex];


            // unrolled loop #1
            if (fingerprint == current.Fingerprint && AreEqual(values[current.ValueIndex], key, comparer))
            {
                return current.ValueIndex;
            }
            fingerprint += DistanceUnit;
            metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
            current = metadata[metadataIndex];

            // unrolled loop #2
            if (fingerprint == current.Fingerprint && AreEqual(values[current.ValueIndex], key, comparer))
            {
                return current.ValueIndex;
            }
            fingerprint += DistanceUnit;
            metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);


            return GetEntryIndexFallback(key, fingerprint, metadataIndex);
        }

        private int GetEntryIndexFallback(T key, uint fingerprint, int metadataIndex)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var metadata = m_Metadata;
            var values = m_Values;
            var comparer = m_Comparer;

            var current = metadata[metadataIndex];

            while (true)
            {
                if (fingerprint == current.Fingerprint)
                {
                    if (AreEqual(values[current.ValueIndex], key, comparer))
                    {
                        return current.ValueIndex;
                    }
                }
                else if (fingerprint > current.Fingerprint)
                {
                    return -1;
                }

                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex, m_Metadata.Length);
                current = metadata[metadataIndex];
            }
        }

        private bool AddEntry(T key, bool overwrite)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var metadata = m_Metadata;
            var values = m_Values;
            var comparer = m_Comparer;

            int hashCode = GetHashCode(key, comparer);
            uint fingerprint = HashCodeToFingerprint(hashCode);
            int metadataIndex = HashCodeToMetadataIndex(hashCode, m_Shifts);


            var current = metadata[metadataIndex];
            while (fingerprint <= current.Fingerprint)
            {
                if (fingerprint == current.Fingerprint &&
                    AreEqual(key, values[current.ValueIndex], comparer))
                {
                    if (overwrite)
                    {
                        values[current.ValueIndex] = key;
                        m_Version++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
                current = metadata[metadataIndex];
            }


            m_Size++;
            values[m_Size - 1] = key;
            PlaceAndShiftUp(new Metadata(fingerprint, m_Size - 1), metadataIndex);


            if (m_Size * MaxLoadFactorDen >= m_Metadata.Length * MaxLoadFactorNum)
            {
                m_Shifts--;
                int newCapacity = 1 << (32 - m_Shifts);
                Resize(newCapacity);
            }

            m_Version++;
            return true;
        }

        private void Resize(int newCapacity)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var oldValues = m_Values;
            var oldMetadata = m_Metadata;

            m_Values = ArrayPool<T>.Shared.Rent(newCapacity);
            oldValues.AsSpan(..m_Size).CopyTo(m_Values.AsSpan());
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(newCapacity);
            m_Metadata.AsSpan().Clear();

            for (int i = 0; i < m_Size; i++)
            {
                (uint fingerprint, int metadataIndex) = NextWhileLess(m_Values[i]);
                PlaceAndShiftUp(new Metadata(fingerprint, i), metadataIndex);
            }

            ArrayPool<T>.Shared.Return(oldValues, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            ArrayPool<Metadata>.Shared.Return(oldMetadata);

            m_Version++;
        }

        private (uint fingerprint, int metadataIndex) NextWhileLess(T key)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }

            var metadata = m_Metadata;

            int hashCode = GetHashCode(key, m_Comparer);
            uint fingerprint = HashCodeToFingerprint(hashCode);
            int metadataIndex = HashCodeToMetadataIndex(hashCode, m_Shifts);

            while (fingerprint < metadata[metadataIndex].Fingerprint)
            {
                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
            }

            return (fingerprint, metadataIndex);
        }

        private void PlaceAndShiftUp(Metadata current, int metadataIndex)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }

            var metadata = m_Metadata;

            while (metadata[metadataIndex].Fingerprint != 0)
            {
                (current, metadata[metadataIndex]) = (metadata[metadataIndex], current);
                current = new Metadata(current.Fingerprint + DistanceUnit, current.ValueIndex);
                metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
            }
            metadata[metadataIndex] = current;
        }

        private bool RemoveEntry(T key)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var metadata = m_Metadata;
            var comparer = m_Comparer;


            (uint fingerprint, int metadataIndex) = NextWhileLess(key);

            while (fingerprint == metadata[metadataIndex].Fingerprint &&
                !AreEqual(m_Values[metadata[metadataIndex].ValueIndex], key, comparer))
            {
                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
            }

            if (fingerprint != metadata[metadataIndex].Fingerprint)
            {
                return false;
            }

            RemoveAt(metadataIndex);
            return true;
        }

        private void RemoveAt(int metadataIndex)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            var metadata = m_Metadata;
            var values = m_Values;


            int valueIndex = metadata[metadataIndex].ValueIndex;

            int nextMetadataIndex = IncrementMetadataIndex(metadataIndex, metadata.Length);
            while (metadata[nextMetadataIndex].Fingerprint >= DistanceUnit * 2)
            {
                metadata[metadataIndex] = new Metadata(metadata[nextMetadataIndex].Fingerprint - DistanceUnit, metadata[nextMetadataIndex].ValueIndex);
                (metadataIndex, nextMetadataIndex) = (nextMetadataIndex, IncrementMetadataIndex(nextMetadataIndex, metadata.Length));
            }

            metadata[metadataIndex] = new Metadata();


            if (valueIndex != m_Size - 1)
            {
                values[valueIndex] = values[m_Size - 1];

                int movingHashCode = GetHashCode(values[valueIndex], m_Comparer);
                int movingMetadataIndex = HashCodeToMetadataIndex(movingHashCode, m_Shifts);

                int valueIndexBack = m_Size - 1;
                while (valueIndexBack != metadata[movingMetadataIndex].ValueIndex)
                {
                    movingMetadataIndex = IncrementMetadataIndex(movingMetadataIndex, metadata.Length);
                }
                metadata[movingMetadataIndex] = new Metadata(metadata[movingMetadataIndex].Fingerprint, valueIndex);
            }

            m_Size--;
            m_Version++;
        }

        private void ClearTable()
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            m_Metadata.AsSpan().Clear();
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Values.AsSpan().Clear();
            }

            m_Size = 0;
            m_Version++;
        }


        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayPoolHashSet<T> m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(ArrayPoolHashSet<T> parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            public readonly T Current
            {
                get
                {
                    if (m_Parent.m_Values is null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                    }
                    if (m_Parent.m_Version != m_Version)
                    {
                        ThrowHelper.ThrowDifferentVersion();
                    }
                    if ((uint)m_Index >= m_Parent.m_Size)
                    {
                        ThrowHelper.ThrowEnumeratorUndefined();
                    }

                    return m_Parent.m_Values[m_Index];
                }
            }

            readonly object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Parent.m_Version != m_Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                if (m_Index >= m_Parent.m_Size)
                {
                    return false;
                }

                return ++m_Index < m_Parent.m_Size;
            }

            public void Reset()
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (m_Parent.m_Version != m_Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                m_Index = -1;
            }
        }

        public readonly struct AlternateLookup<TAlternate>
        {
            private readonly ArrayPoolHashSet<T> m_Parent;

            internal AlternateLookup(ArrayPoolHashSet<T> parent)
            {
                m_Parent = parent;
            }

            public ArrayPoolHashSet<T> Set => m_Parent;

            public bool Add(TAlternate item)
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }

                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternate, T>;
                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                // TODO: opt.
                var key = comparer.Create(item);
                return m_Parent.Add(key);
            }

            public bool Contains(TAlternate item)
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }

                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternate, T>;
                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                // TODO: opt.
                var key = comparer.Create(item);
                return m_Parent.Contains(key);
            }

            public bool Remove(TAlternate item)
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }

                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternate, T>;
                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                // TODO: opt.
                var key = comparer.Create(item);
                return m_Parent.Remove(key);
            }

            public bool TryGetValue(TAlternate equalValue, out T actualValue)
            {
                if (m_Parent.m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }

                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternate, T>;
                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                // TODO: opt. we should not Create() if it is avoidable
                var key = comparer.Create(equalValue);
                return m_Parent.TryGetValue(key, out actualValue);
            }
        }









        public int Count
        {
            get
            {
                if (m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                return m_Size;
            }
        }

        public bool IsReadOnly => false;

        public int Capacity
        {
            get
            {
                if (m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                return m_Values.Length;
            }
        }

        public IEqualityComparer<T> Comparer
        {
            get
            {
                if (m_Values is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                if (m_Comparer == null)
                {
                    return EqualityComparer<T>.Default;
                }

                return m_Comparer;
            }
        }




        public ArrayPoolHashSet() : this(16) { }
        public ArrayPoolHashSet(IEqualityComparer<T> comparer) : this(16, comparer) { }
        public ArrayPoolHashSet(int capacity) : this(capacity, typeof(T).IsValueType ? null : EqualityComparer<T>.Default) { }
        public ArrayPoolHashSet(int capacity, IEqualityComparer<T>? comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            m_Values = ArrayPool<T>.Shared.Rent(capacity);
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(capacity);
            m_Metadata.AsSpan().Clear();

            m_Size = 0;
            m_Shifts = 32 - CollectionHelper.TrailingZeroCount((ulong)m_Values.Length);

            if (!typeof(T).IsValueType)
            {
                comparer ??= EqualityComparer<T>.Default;
            }
            m_Comparer = comparer;
        }

        public ArrayPoolHashSet(IEnumerable<T> source) : this(source, typeof(T).IsValueType ? null : EqualityComparer<T>.Default) { }
        public ArrayPoolHashSet(IEnumerable<T> source, IEqualityComparer<T>? comparer)
        {
            if (source is ArrayPoolHashSet<T> cloneSource && cloneSource.m_Comparer == comparer)
            {
                if (cloneSource.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(source));
                }
                if (cloneSource.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(source));
                }

                m_Values = ArrayPool<T>.Shared.Rent(cloneSource.m_Values.Length);
                m_Metadata = ArrayPool<Metadata>.Shared.Rent(cloneSource.m_Metadata.Length);
                cloneSource.m_Values.AsSpan().CopyTo(m_Values);
                cloneSource.m_Metadata.AsSpan().CopyTo(m_Metadata);

                m_Size = cloneSource.m_Size;
                m_Shifts = cloneSource.m_Shifts;

                m_Comparer = comparer;
                return;
            }

            // TODO: should be count * den / num?
            int capacity =
                CollectionHelper.TryGetNonEnumeratedCount(source, out int count) ?
                (int)(count * MaxLoadFactorDen / MaxLoadFactorNum) :
                16;
            m_Values = ArrayPool<T>.Shared.Rent(capacity);
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(capacity);
            m_Metadata.AsSpan().Clear();

            m_Size = 0;
            m_Shifts = 32 - CollectionHelper.TrailingZeroCount((ulong)m_Values.Length);

            if (!typeof(T).IsValueType)
            {
                comparer ??= EqualityComparer<T>.Default;
            }
            m_Comparer = comparer;

            foreach (var pair in source)
            {
                AddEntry(pair, false);
            }
        }




        public bool Add(T item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return AddEntry(item, false);
        }

        public void Clear()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            ClearTable();
        }

        public bool Contains(T item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return GetEntryIndex(item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (array.Length - arrayIndex < m_Size)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, m_Size - array.Length, arrayIndex);
            }

            m_Values.AsSpan(..m_Size).CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(T[] array, int arrayIndex, int count)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Size, count);
            }
            if (array.Length - arrayIndex < count)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, count - array.Length, arrayIndex);
            }

            m_Values.AsSpan(..count).CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(Span<T> span)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            m_Values.AsSpan(..m_Size).CopyTo(span);
        }

        public IEqualityComparer<ArrayPoolHashSet<T>> CreateSetComparer()
        {
            throw new NotImplementedException();
        }

        public int EnsureCapacity(int capacity)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            m_Version++;

            if (capacity < m_Values.Length)
            {
                return Capacity;
            }

            Resize(capacity);
            return Capacity;
        }

        public void Dispose()
        {
            if (m_Values != null)
            {
                ArrayPool<T>.Shared.Return(m_Values, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Values = null;
            }
            if (m_Metadata != null)
            {
                ArrayPool<Metadata>.Shared.Return(m_Metadata);
                m_Metadata = null;
            }
            m_Size = 0;
            m_Version = int.MinValue;
        }

        public void ExceptWith(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Size == 0)
            {
                return;
            }
            if (other == this)
            {
                ClearTable();
                return;
            }

            foreach (var element in other)
            {
                RemoveEntry(element);
            }
        }

        public AlternateLookup<TAlternate> GetAlternateLookup<TAlternate>()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Comparer is not IAlternateEqualityComparer<TAlternate, T>)
            {
                ThrowHelper.ThrowNotAlternateComparer();
            }

            return new AlternateLookup<TAlternate>(this);
        }

        public Enumerator GetEnumerator()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this || m_Size == 0)
            {
                return;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return;
                }
            }

            IntersectWithIEnumerable(other);
        }

        private void IntersectWithIEnumerable(IEnumerable<T> other)
        {
            throw new NotImplementedException();
            foreach (var element in other)
            {

            }
        }

        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                return false;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return false;
                }
                if (m_Size == 0)
                {
                    return otherCount > 0;
                }
            }

            throw new NotImplementedException();
        }

        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Size == 0 || other == this)
            {
                return false;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return true;
                }
            }

            throw new NotImplementedException();
        }

        public bool IsSubsetOf(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Size == 0 || other == this)
            {
                return true;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return false;
                }
            }

            throw new NotImplementedException();
        }

        public bool IsSupersetOf(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                return true;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return true;
                }
                if (m_Size == 0)
                {
                    return false;
                }
            }

            throw new NotImplementedException();
        }

        public bool Overlaps(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                return true;
            }
            if (m_Size == 0)
            {
                return false;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return false;
                }
            }

            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return RemoveEntry(item);
        }

        public int RemoveWhere(Predicate<T> predicate)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            int removedCount = 0;
            for (int i = 0; i < m_Size; i++)
            {
                if (predicate(m_Values[i]))
                {
                    RemoveEntry(m_Values[i]);
                    removedCount++;
                    i--;
                }
            }
            return removedCount;
        }

        public bool SetEquals(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                return true;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (m_Size == 0)
                {
                    return otherCount == 0;
                }
            }

            throw new NotImplementedException();
        }

        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                ClearTable();
                return;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return;
                }
            }

            SymmetricExceptWithIEnumerable(other);
        }

        private void SymmetricExceptWithIEnumerable(IEnumerable<T> other)
        {
            foreach (var element in other)
            {
                if (!AddEntry(element, false))
                {
                    RemoveEntry(element);
                }
            }
        }

        public override string ToString()
        {
            return $"{Count} items";
        }

        public void TrimExcess()
        {
            TrimExcess((int)(Count * MaxLoadFactorDen / MaxLoadFactorNum));
        }

        public void TrimExcess(int capacity)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (capacity < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), Count, int.MaxValue, capacity);
            }

            Resize(Math.Max(capacity, 16));
        }

        public bool TryGetAlternateLookup<TAlternate>(out AlternateLookup<TAlternate> lookup)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Comparer is not IAlternateEqualityComparer<TAlternate, T>)
            {
                lookup = default;
                return false;
            }

            lookup = new AlternateLookup<TAlternate>(this);
            return true;

        }

        public bool TryGetValue(T equalValue, out T actualValue)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            if (GetEntryIndex(equalValue) is int index && index >= 0)
            {
                actualValue = m_Values[index];
                return true;
            }

            actualValue = default!;
            return false;
        }

        public void UnionWith(IEnumerable<T> other)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (other == this)
            {
                return;
            }
            if (CollectionHelper.TryGetNonEnumeratedCount(other, out int otherCount))
            {
                if (otherCount == 0)
                {
                    return;
                }
            }

            UnionWithIEnumerable(other);
        }

        private void UnionWithIEnumerable(IEnumerable<T> other)
        {
            foreach (var element in other)
            {
                AddEntry(element, false);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<T>.Add(T item)
        {
            Add(item);
        }
    }
}
