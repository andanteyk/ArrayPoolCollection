using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary, IDisposable
        where TKey : notnull
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
        private const int HashMixer = -1371748571;


        private KeyValuePair<TKey, TValue>[]? m_Values;
        private Metadata[]? m_Metadata;

        private int m_Size;
        private int m_Version;

        private readonly IEqualityComparer<TKey>? m_Comparer;



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetShifts(int length)
        {
            return 32 - CollectionHelper.TrailingZeroCount((ulong)length);
        }

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
        private static bool AreEqual(TKey key1, TKey key2, IEqualityComparer<TKey>? comparer)
        {
            if (typeof(TKey).IsValueType)
            {
                if (comparer == null)
                {
                    return EqualityComparer<TKey>.Default.Equals(key1, key2);
                }

                return comparer.Equals(key1, key2);
            }
            else
            {
                return comparer!.Equals(key1, key2);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetHashCode(TKey key, IEqualityComparer<TKey>? comparer)
        {
            if (typeof(TKey).IsValueType)
            {
                if (comparer == null)
                {
                    return key.GetHashCode() * HashMixer;
                }

                return comparer.GetHashCode(key) * HashMixer;
            }
            else
            {
                return comparer!.GetHashCode(key) * HashMixer;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int IncrementMetadataIndex(int metadataIndex)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }

            return (metadataIndex + 1) & (m_Metadata.Length - 1);
        }

        private int GetEntryIndex(TKey key)
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
            int metadataIndex = HashCodeToMetadataIndex(hashCode, GetShifts(m_Values.Length));

            var current = metadata[metadataIndex];


            // unrolled loop #1
            if (fingerprint == current.Fingerprint && AreEqual(values[current.ValueIndex].Key, key, comparer))
            {
                return current.ValueIndex;
            }
            fingerprint += DistanceUnit;
            metadataIndex = IncrementMetadataIndex(metadataIndex);
            current = metadata[metadataIndex];

            // unrolled loop #2
            if (fingerprint == current.Fingerprint && AreEqual(values[current.ValueIndex].Key, key, comparer))
            {
                return current.ValueIndex;
            }
            fingerprint += DistanceUnit;
            metadataIndex = IncrementMetadataIndex(metadataIndex);


            return GetEntryIndexFallback(key, fingerprint, metadataIndex);
        }

        private int GetEntryIndexFallback(TKey key, uint fingerprint, int metadataIndex)
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
                    if (AreEqual(values[current.ValueIndex].Key, key, comparer))
                    {
                        return current.ValueIndex;
                    }
                }
                else if (fingerprint > current.Fingerprint)
                {
                    return -1;
                }

                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex);
                current = metadata[metadataIndex];
            }
        }

        private bool AddEntry(TKey key, TValue value, bool overwrite)
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
            int metadataIndex = HashCodeToMetadataIndex(hashCode, GetShifts(m_Values.Length));


            var current = metadata[metadataIndex];
            while (fingerprint <= current.Fingerprint)
            {
                if (fingerprint == current.Fingerprint &&
                    AreEqual(key, values[current.ValueIndex].Key, comparer))
                {
                    if (overwrite)
                    {
                        values[current.ValueIndex] = new KeyValuePair<TKey, TValue>(key, value);
                        m_Version++;
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex);
                current = metadata[metadataIndex];
            }


            m_Size++;
            values[m_Size - 1] = new KeyValuePair<TKey, TValue>(key, value);
            PlaceAndShiftUp(new Metadata(fingerprint, m_Size - 1), metadataIndex);


            if (m_Size * MaxLoadFactorDen >= m_Metadata.Length * MaxLoadFactorNum)
            {
                Resize(m_Values.Length << 1);
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

            m_Values = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(newCapacity);
            oldValues.AsSpan(..m_Size).CopyTo(m_Values.AsSpan());
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(newCapacity);
            m_Metadata.AsSpan().Clear();

            for (int i = 0; i < m_Size; i++)
            {
                (uint fingerprint, int metadataIndex) = NextWhileLess(m_Values[i].Key);
                PlaceAndShiftUp(new Metadata(fingerprint, i), metadataIndex);
            }

            ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(oldValues, RuntimeHelpers.IsReferenceOrContainsReferences<KeyValuePair<TKey, TValue>>());
            ArrayPool<Metadata>.Shared.Return(oldMetadata);

            m_Version++;
        }

        private (uint fingerprint, int metadataIndex) NextWhileLess(TKey key)
        {
            if (m_Metadata == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
            }
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            int hashCode = GetHashCode(key, m_Comparer);
            uint fingerprint = HashCodeToFingerprint(hashCode);
            int metadataIndex = HashCodeToMetadataIndex(hashCode, GetShifts(m_Values.Length));

            while (fingerprint < m_Metadata[metadataIndex].Fingerprint)
            {
                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex);
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
                metadataIndex = IncrementMetadataIndex(metadataIndex);
            }
            metadata[metadataIndex] = current;
        }

        private bool RemoveEntry(TKey key)
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
                !AreEqual(m_Values[metadata[metadataIndex].ValueIndex].Key, key, comparer))
            {
                fingerprint += DistanceUnit;
                metadataIndex = IncrementMetadataIndex(metadataIndex);
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
            int shifts = GetShifts(m_Values.Length);


            int valueIndex = metadata[metadataIndex].ValueIndex;

            int nextMetadataIndex = IncrementMetadataIndex(metadataIndex);
            while (metadata[nextMetadataIndex].Fingerprint >= DistanceUnit * 2)
            {
                metadata[metadataIndex] = new Metadata(metadata[nextMetadataIndex].Fingerprint - DistanceUnit, metadata[nextMetadataIndex].ValueIndex);
                (metadataIndex, nextMetadataIndex) = (nextMetadataIndex, IncrementMetadataIndex(nextMetadataIndex));
            }

            metadata[metadataIndex] = new Metadata();


            if (valueIndex != m_Size - 1)
            {
                values[valueIndex] = values[m_Size - 1];

                int movingHashCode = GetHashCode(values[valueIndex].Key, m_Comparer);
                int movingMetadataIndex = HashCodeToMetadataIndex(movingHashCode, shifts);

                int valueIndexBack = m_Size - 1;
                while (valueIndexBack != metadata[movingMetadataIndex].ValueIndex)
                {
                    movingMetadataIndex = IncrementMetadataIndex(movingMetadataIndex);
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
            if (RuntimeHelpers.IsReferenceOrContainsReferences<KeyValuePair<TKey, TValue>>())
            {
                m_Values.AsSpan().Clear();
            }

            m_Size = 0;
            m_Version++;
        }



        public readonly struct KeyCollection : ICollection<TKey>, ICollection, IReadOnlyCollection<TKey>
        {
            private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;

            internal KeyCollection(ArrayPoolDictionary<TKey, TValue> parent)
            {
                m_Parent = parent;
            }

            public int Count => m_Parent.Count;

            public bool IsReadOnly => true;

            int ICollection.Count => m_Parent.Count;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)m_Parent).SyncRoot;

            public void Add(TKey item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TKey item)
            {
                return m_Parent.ContainsKey(item);
            }

            public void CopyTo(TKey[] array, int arrayIndex)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (array.Length - arrayIndex < m_Parent.m_Size)
                {
                    ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, m_Parent.m_Size - array.Length, arrayIndex);
                }

                for (int i = 0; i < m_Parent.m_Size; i++)
                {
                    array[arrayIndex + i] = m_Parent.m_Values[i].Key;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_Parent);
            }

            IEnumerator<TKey> IEnumerable<TKey>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Remove(TKey item)
            {
                throw new NotSupportedException();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (array.Length - index < m_Parent.m_Size)
                {
                    ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Parent.m_Size - array.Length, index);
                }
                if (array.Rank > 1)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                if (array is not TKey[] typedArray)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                else
                {
                    for (int i = 0; i < m_Parent.m_Size; i++)
                    {
                        typedArray[index + i] = m_Parent.m_Values[i].Key;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<TKey>
            {
                private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;
                private readonly int m_Version;
                private int m_Index;

                internal Enumerator(ArrayPoolDictionary<TKey, TValue> parent)
                {
                    m_Parent = parent;
                    m_Version = parent.m_Version;
                    m_Index = -1;
                }

                public readonly TKey Current
                {
                    get
                    {
                        if (m_Parent.m_Values == null)
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

                        return m_Parent.m_Values[m_Index].Key;
                    }
                }

                readonly object IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (m_Parent.m_Values == null)
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
                    if (m_Parent.m_Values == null)
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
        }

        public readonly struct ValueCollection : ICollection<TValue>, ICollection, IReadOnlyCollection<TValue>
        {
            private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;

            internal ValueCollection(ArrayPoolDictionary<TKey, TValue> parent)
            {
                m_Parent = parent;
            }

            public int Count => m_Parent.Count;

            public bool IsReadOnly => true;

            int ICollection.Count => m_Parent.Count;

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => ((ICollection)m_Parent).SyncRoot;

            public void Add(TValue item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Contains(TValue item)
            {
                return m_Parent.ContainsValue(item);
            }

            public void CopyTo(TValue[] array, int arrayIndex)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (array.Length - arrayIndex < m_Parent.m_Size)
                {
                    ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, m_Parent.m_Size - array.Length, arrayIndex);
                }

                for (int i = 0; i < m_Parent.m_Size; i++)
                {
                    array[arrayIndex + i] = m_Parent.m_Values[i].Value;
                }
            }

            public Enumerator GetEnumerator()
            {
                return new Enumerator(m_Parent);
            }

            IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator()
            {
                return GetEnumerator();
            }

            public bool Remove(TValue item)
            {
                throw new NotSupportedException();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (array.Length - index < m_Parent.m_Size)
                {
                    ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Parent.m_Size - array.Length, index);
                }
                if (array.Rank > 1)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                if (array is not TValue[] typedArray)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                else
                {
                    for (int i = 0; i < m_Parent.m_Size; i++)
                    {
                        typedArray[index + i] = m_Parent.m_Values[i].Value;
                    }
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            public struct Enumerator : IEnumerator<TValue>
            {
                private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;
                private readonly int m_Version;
                private int m_Index;

                internal Enumerator(ArrayPoolDictionary<TKey, TValue> parent)
                {
                    m_Parent = parent;
                    m_Version = parent.m_Version;
                    m_Index = -1;
                }

                public readonly TValue Current
                {
                    get
                    {
                        if (m_Parent.m_Values == null)
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

                        return m_Parent.m_Values[m_Index].Value;
                    }
                }

                readonly object? IEnumerator.Current => Current;

                public void Dispose()
                {
                }

                public bool MoveNext()
                {
                    if (m_Parent.m_Values == null)
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
                    if (m_Parent.m_Values == null)
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
        }

        public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>, IDictionaryEnumerator
        {
            private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(ArrayPoolDictionary<TKey, TValue> parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            public readonly KeyValuePair<TKey, TValue> Current
            {
                get
                {
                    if (m_Parent.m_Values == null)
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

            DictionaryEntry IDictionaryEnumerator.Entry => new DictionaryEntry(Current.Key, Current.Value);

            object IDictionaryEnumerator.Key => Current.Key;

            object? IDictionaryEnumerator.Value => Current.Value;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Parent.m_Values == null)
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
                if (m_Parent.m_Values == null)
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

        public readonly struct AlternateLookup<TAlternateKey>
            where TAlternateKey : notnull
        {
            private readonly ArrayPoolDictionary<TKey, TValue> m_Parent;

            internal AlternateLookup(ArrayPoolDictionary<TKey, TValue> parent)
            {
                m_Parent = parent;
            }

            public ArrayPoolDictionary<TKey, TValue> Dictionary => m_Parent;

            public TValue this[TAlternateKey alternateKey]
            {
                get
                {
                    if (m_Parent.m_Values == null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                    }

                    if (GetAlternateEntryIndex(alternateKey) is int index && index >= 0)
                    {
                        return m_Parent.m_Values[index].Value;
                    }

                    ThrowHelper.ThrowKeyNotFound(alternateKey);
                    return default;
                }
                set
                {
                    if (m_Parent.m_Values == null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                    }

                    var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;
                    if (comparer == null)
                    {
                        ThrowHelper.ThrowNotAlternateComparer();
                    }

                    var key = comparer.Create(alternateKey);
                    m_Parent.AddEntry(key, value, true);
                }
            }

            private int GetAlternateEntryIndex(TAlternateKey key)
            {
                if (m_Parent.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Metadata));
                }
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                var metadata = m_Parent.m_Metadata;
                var values = m_Parent.m_Values;
                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;

                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                int hashCode = comparer.GetHashCode(key) * HashMixer;
                uint fingerprint = HashCodeToFingerprint(hashCode);
                int metadataIndex = HashCodeToMetadataIndex(hashCode, GetShifts(m_Parent.m_Values.Length));

                var current = metadata[metadataIndex];


                // unrolled loop #1
                if (fingerprint == current.Fingerprint && comparer.Equals(key, values[current.ValueIndex].Key))
                {
                    return current.ValueIndex;
                }
                fingerprint += DistanceUnit;
                metadataIndex = m_Parent.IncrementMetadataIndex(metadataIndex);
                current = metadata[metadataIndex];

                // unrolled loop #2
                if (fingerprint == current.Fingerprint && comparer.Equals(key, values[current.ValueIndex].Key))
                {
                    return current.ValueIndex;
                }
                fingerprint += DistanceUnit;
                metadataIndex = m_Parent.IncrementMetadataIndex(metadataIndex);


                return GetAlternateEntryIndexFallback(key, fingerprint, metadataIndex);
            }

            private int GetAlternateEntryIndexFallback(TAlternateKey key, uint fingerprint, int metadataIndex)
            {
                if (m_Parent.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Metadata));
                }
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                var metadata = m_Parent.m_Metadata;
                var values = m_Parent.m_Values;
                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;

                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                var current = metadata[metadataIndex];

                while (true)
                {
                    if (fingerprint == current.Fingerprint)
                    {
                        if (comparer.Equals(key, values[current.ValueIndex].Key))
                        {
                            return current.ValueIndex;
                        }
                    }
                    else if (fingerprint > current.Fingerprint)
                    {
                        return -1;
                    }

                    fingerprint += DistanceUnit;
                    metadataIndex = m_Parent.IncrementMetadataIndex(metadataIndex);
                    current = metadata[metadataIndex];
                }
            }

            private bool RemoveEntry(TAlternateKey key)
            {
                if (m_Parent.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Metadata));
                }
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                var metadata = m_Parent.m_Metadata;
                var values = m_Parent.m_Values;
                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;

                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                (uint fingerprint, int metadataIndex) = NextWhileLess(key);

                while (fingerprint == metadata[metadataIndex].Fingerprint &&
                    !comparer.Equals(key, values[metadata[metadataIndex].ValueIndex].Key))
                {
                    fingerprint += DistanceUnit;
                    metadataIndex = m_Parent.IncrementMetadataIndex(metadataIndex);
                }

                if (fingerprint != metadata[metadataIndex].Fingerprint)
                {
                    return false;
                }

                m_Parent.RemoveAt(metadataIndex);
                return true;
            }

            private (uint fingerprint, int metadataIndex) NextWhileLess(TAlternateKey key)
            {
                if (m_Parent.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Metadata));
                }
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                var metadata = m_Parent.m_Metadata;
                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;

                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                int hashCode = comparer.GetHashCode(key) * HashMixer;
                uint fingerprint = HashCodeToFingerprint(hashCode);
                int metadataIndex = HashCodeToMetadataIndex(hashCode, GetShifts(m_Parent.m_Values.Length));

                while (fingerprint < metadata[metadataIndex].Fingerprint)
                {
                    fingerprint += DistanceUnit;
                    metadataIndex = m_Parent.IncrementMetadataIndex(metadataIndex);
                }

                return (fingerprint, metadataIndex);
            }

            public bool ContainsKey(TAlternateKey alternateKey)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                return GetAlternateEntryIndex(alternateKey) >= 0;
            }

            public bool Remove(TAlternateKey alternateKey)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                return RemoveEntry(alternateKey);
            }

            public bool Remove(TAlternateKey alternateKey, [MaybeNullWhen(false)] out TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                if (GetAlternateEntryIndex(alternateKey) is int index && index >= 0)
                {
                    key = m_Parent.m_Values[index].Key;
                    value = m_Parent.m_Values[index].Value;

                    // TODO: opt.
                    RemoveEntry(alternateKey);
                    return true;
                }

                key = default;
                value = default;
                return false;
            }

            public bool TryAdd(TAlternateKey alternateKey, TValue value)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                var comparer = m_Parent.m_Comparer as IAlternateEqualityComparer<TAlternateKey, TKey>;
                if (comparer == null)
                {
                    ThrowHelper.ThrowNotAlternateComparer();
                }

                var key = comparer.Create(alternateKey);
                return m_Parent.AddEntry(key, value, false);
            }

            public bool TryGetValue(TAlternateKey alternateKey, [MaybeNullWhen(false)] out TKey key, [MaybeNullWhen(false)] out TValue value)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                if (GetAlternateEntryIndex(alternateKey) is int index && index >= 0)
                {
                    key = m_Parent.m_Values[index].Key;
                    value = m_Parent.m_Values[index].Value;
                    return true;
                }

                key = default;
                value = default;
                return false;
            }

            public bool TryGetValue(TAlternateKey alternateKey, [MaybeNullWhen(false)] out TValue value)
            {
                if (m_Parent.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Values));
                }

                if (GetAlternateEntryIndex(alternateKey) is int index && index >= 0)
                {
                    value = m_Parent.m_Values[index].Value;
                    return true;
                }

                value = default;
                return false;
            }
        }



        public ArrayPoolDictionary() : this(16, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default) { }
        public ArrayPoolDictionary(IEqualityComparer<TKey>? comparer) : this(16, comparer) { }
        public ArrayPoolDictionary(int capacity) : this(capacity, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default) { }
        public ArrayPoolDictionary(int capacity, IEqualityComparer<TKey>? comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            m_Values = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(capacity);
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(capacity);
            m_Metadata.AsSpan().Clear();

            m_Size = 0;

            if (!typeof(TKey).IsValueType)
            {
                comparer ??= EqualityComparer<TKey>.Default;
            }
            m_Comparer = comparer;
        }

        public ArrayPoolDictionary(IDictionary<TKey, TValue> source) : this(source, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default) { }
        public ArrayPoolDictionary(IDictionary<TKey, TValue> source, IEqualityComparer<TKey>? comparer)
        {
            if (source is ArrayPoolDictionary<TKey, TValue> cloneSource && cloneSource.m_Comparer == comparer)
            {
                if (cloneSource.m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(source));
                }
                if (cloneSource.m_Metadata == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(source));
                }

                m_Values = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(cloneSource.m_Values.Length);
                m_Metadata = ArrayPool<Metadata>.Shared.Rent(cloneSource.m_Metadata.Length);
                cloneSource.m_Values.AsSpan().CopyTo(m_Values);
                cloneSource.m_Metadata.AsSpan().CopyTo(m_Metadata);

                m_Size = cloneSource.m_Size;

                m_Comparer = comparer;
                return;
            }

            // TODO: should be count * den / num?
            int capacity = (int)(source.Count * MaxLoadFactorDen / MaxLoadFactorNum);
            m_Values = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(capacity);
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(capacity);
            m_Metadata.AsSpan().Clear();

            m_Size = 0;

            if (!typeof(TKey).IsValueType)
            {
                comparer ??= EqualityComparer<TKey>.Default;
            }
            m_Comparer = comparer;

            foreach (var pair in source)
            {
                AddEntry(pair.Key, pair.Value, false);
            }
        }

        public ArrayPoolDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source) : this(source, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default) { }
        public ArrayPoolDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                // TODO: opt. by segmentedArray?
                count = 8;
            }

            // TODO: should be count * den / num?
            count = (int)(count * MaxLoadFactorDen / MaxLoadFactorNum);
            m_Values = ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Rent(count);
            m_Metadata = ArrayPool<Metadata>.Shared.Rent(count);
            m_Metadata.AsSpan().Clear();

            m_Size = 0;

            m_Comparer = comparer;

            foreach (var pair in source)
            {
                AddEntry(pair.Key, pair.Value, false);
            }
        }



        public TValue this[TKey key]
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentIsNull(nameof(key));
                }

                if (GetEntryIndex(key) is int index && index >= 0)
                {
                    return m_Values[index].Value;
                }

                ThrowHelper.ThrowKeyNotFound(key);
                return default;
            }
            set
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentIsNull(nameof(key));
                }

                AddEntry(key, value, true);
            }
        }

        object? IDictionary.this[object key]
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }
                if (key is null)
                {
                    ThrowHelper.ThrowArgumentIsNull(nameof(key));
                }
                if (key is not TKey typedKey)
                {
                    return null;
                }
                else
                {
                    if (GetEntryIndex(typedKey) is int index && index >= 0)
                    {
                        return m_Values[index].Value;
                    }
                    else
                    {
                        return null;
                    }
                }
            }
            set
            {
                if (key == null)
                {
                    ThrowHelper.ThrowArgumentIsNull(nameof(key));
                }
                if (key is not TKey typedKey)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(key));
                }
                else if (value is not TValue typedValue && !(value is null && default(TValue) is null))
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
                }
                else
                {
                    AddEntry(typedKey, (TValue)value!, true);
                }
            }
        }

        public KeyCollection Keys
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }
                return new KeyCollection(this);
            }
        }
        ICollection<TKey> IDictionary<TKey, TValue>.Keys => Keys;

        public ValueCollection Values
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }
                return new ValueCollection(this);
            }
        }
        ICollection<TValue> IDictionary<TKey, TValue>.Values => Values;

        public int Capacity
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                return m_Values.Length;
            }
        }

        public IEqualityComparer<TKey> Comparer
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                if (m_Comparer == null)
                {
                    return EqualityComparer<TKey>.Default;
                }

                return m_Comparer;
            }
        }

        public int Count
        {
            get
            {
                if (m_Values == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
                }

                return m_Size;
            }
        }

        public bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => Keys;

        ICollection IDictionary.Keys => Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => Values;

        ICollection IDictionary.Values => Values;

        bool IDictionary.IsFixedSize => false;

        bool IDictionary.IsReadOnly => false;

        int ICollection.Count => Count;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public void Add(TKey key, TValue value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (!AddEntry(key, value, false))
            {
                ThrowHelper.ThrowKeyIsAlreadyExists(key);
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (!AddEntry(item.Key, item.Value, false))
            {
                ThrowHelper.ThrowKeyIsAlreadyExists(item.Key);
            }
        }

        public void Clear()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            ClearTable();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return GetEntryIndex(item.Key) is int index && index >= 0 &&
                EqualityComparer<TValue>.Default.Equals(item.Value, m_Values[index].Value);
        }

        public bool ContainsKey(TKey key)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return GetEntryIndex(key) >= 0;
        }

        public bool ContainsValue(TValue value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            foreach (var pair in m_Values.AsSpan(..m_Size))
            {
                if (EqualityComparer<TValue>.Default.Equals(pair.Value, value))
                {
                    return true;
                }
            }
            return false;
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

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
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

        public AlternateLookup<TAlternateKey> GetAlternateLookup<TAlternateKey>()
            where TAlternateKey : notnull
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Comparer is not IAlternateEqualityComparer<TAlternateKey, TKey>)
            {
                ThrowHelper.ThrowNotAlternateComparer();
            }

            return new AlternateLookup<TAlternateKey>(this);
        }

        public Enumerator GetEnumerator()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return new Enumerator(this);
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return RemoveEntry(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            if (GetEntryIndex(item.Key) is int index && index >= 0 &&
                EqualityComparer<TValue>.Default.Equals(item.Value, m_Values[index].Value))
            {
                RemoveEntry(item.Key);
                return true;
            }

            return false;
        }

        public bool Remove(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            if (GetEntryIndex(key) is int index && index >= 0)
            {
                value = m_Values[index].Value;
                RemoveEntry(key);
                return true;
            }

            value = default;
            return false;
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

        public bool TryAdd(TKey key, TValue value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            return AddEntry(key, value, false);
        }

        public bool TryGetAlternateLookup<TAlternateKey>(out AlternateLookup<TAlternateKey> alternateLookup)
            where TAlternateKey : notnull
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (m_Comparer is not IAlternateEqualityComparer<TAlternateKey, TKey>)
            {
                alternateLookup = default;
                return false;
            }

            alternateLookup = new AlternateLookup<TAlternateKey>(this);
            return true;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            if (GetEntryIndex(key) is int index && index >= 0)
            {
                value = m_Values[index].Value;
                return true;
            }
            value = default!;
            return false;
        }

        void IDictionary.Add(object key, object? value)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (key is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(key));
            }
            if (key is not TKey typedKey)
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(key));
                return;
            }
            if (value is not TValue typedValue && !(value is null && default(TValue) is null))
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
                return;
            }

            Add(typedKey, (TValue)value!);
        }

        void IDictionary.Clear()
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }

            ClearTable();
        }

        bool IDictionary.Contains(object key)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (key is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(key));
            }
            if (key is not TKey typedKey)
            {
                return false;
            }
            else
            {
                return ContainsKey(typedKey);
            }
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (array is KeyValuePair<TKey, TValue>[] typedArray)
            {
                CopyTo(typedArray, index);
            }
            else if (array is object[] objectArray)
            {
                try
                {
                    Array.Copy(m_Values, 0, objectArray, index, m_Size);
                }
                catch (ArrayTypeMismatchException)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
            }
            else
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        IDictionaryEnumerator IDictionary.GetEnumerator()
        {
            return GetEnumerator();
        }

        void IDictionary.Remove(object key)
        {
            if (m_Values == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Values));
            }
            if (key is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(key));
            }
            if (key is not TKey typedKey)
            {
                return;
            }
            else
            {
                Remove(typedKey);
            }
        }

        public void Dispose()
        {
            if (m_Values != null)
            {
                ArrayPool<KeyValuePair<TKey, TValue>>.Shared.Return(m_Values, RuntimeHelpers.IsReferenceOrContainsReferences<KeyValuePair<TKey, TValue>>());
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

        public override string ToString()
        {
            return $"{m_Size} items";
        }
    }
}
