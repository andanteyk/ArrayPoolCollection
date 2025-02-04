using System.Buffers;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolList<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable
    {
        private T[]? m_Array;
        private int m_Length;
        private int m_Version;

        public ArrayPoolList()
        {
            m_Array = ArrayPool<T>.Shared.Rent(16);
            m_Length = 0;
            m_Version = 0;
        }

        public ArrayPoolList(int capacity)
        {
            m_Array = ArrayPool<T>.Shared.Rent(capacity);
            m_Length = 0;
            m_Version = 0;
        }

        public ArrayPoolList(IEnumerable<T> source)
        {
            // TODO: may be able to optimize if source is ICollection(<T>)
            var segmentedArray = new SegmentedArray<T>(SegmentedArray<T>.Stack16.Create().AsSpan());

            segmentedArray.AddRange(source);
            m_Array = segmentedArray.ToArrayPool(out var span);
            m_Length = span.Length;
            m_Version = 0;
        }

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
            set
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                if (value < m_Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRange(nameof(value), m_Length, CollectionHelper.ArrayMaxLength, value);
                }

                int newLength = CollectionHelper.RoundUpToPowerOf2(Math.Max(value, 16));
                if (newLength < 0)
                {
                    newLength = CollectionHelper.ArrayMaxLength;
                }

                if (newLength != m_Array.Length)
                {
                    var newArray = ArrayPool<T>.Shared.Rent(newLength);
                    m_Array.AsSpan(..m_Length).CopyTo(newArray);
                    ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                    m_Array = newArray;
                    m_Version++;
                }
            }
        }


        public T this[int index]
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

                return m_Array[index];
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

                m_Array[index] = value;
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

        bool ICollection<T>.IsReadOnly => false;
        bool IList.IsReadOnly => false;

        bool IList.IsFixedSize => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object? IList.this[int index]
        {
            get => this[index];
            set
            {
                if (value is T typedValue)
                {
                    this[index] = typedValue;
                    return;
                }
                if (value is null && default(T) is null)
                {
                    this[index] = default!;
                    return;
                }

                ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            }
        }

        private void Grow(int leastSize)
        {
            var newArray = ArrayPool<T>.Shared.Rent(leastSize);
            m_Array.AsSpan().CopyTo(newArray);
            ArrayPool<T>.Shared.Return(m_Array!, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            m_Array = newArray;

            m_Version++;
        }

        public void Add(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length + 1 > m_Array.Length)
            {
                Grow(m_Array.Length + 1);
            }
            m_Array[m_Length++] = item;
            m_Version++;
        }

        public void AddRange(IEnumerable<T> source)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                if (m_Length + count > m_Array.Length)
                {
                    Grow(m_Length + count);
                }

                if (source is ICollection<T> genericCollection)
                {
                    genericCollection.CopyTo(m_Array, m_Length);
                    m_Length += count;
                }
                else if (source is ICollection collection)
                {
                    collection.CopyTo(m_Array, m_Length);
                    m_Length += count;
                }
                else
                {
                    foreach (var element in source)
                    {
                        m_Array[m_Length++] = element;
                    }
                }
            }
            else
            {
                foreach (var element in source)
                {
                    if (m_Length + 1 > m_Array.Length)
                    {
                        Grow(m_Array.Length + 1);
                    }
                    m_Array[m_Length++] = element;
                }
            }

            m_Version++;
        }

        public void AddRange(T[] source) => AddRange(source.AsSpan());

        public void AddRange(ReadOnlySpan<T> source)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length + source.Length > m_Array.Length)
            {
                Grow(m_Length + source.Length);
            }
            source.CopyTo(m_Array.AsSpan(m_Length..));
            m_Length += source.Length;
            m_Version++;
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return new ReadOnlyCollection<T>(this);
        }

        /// <summary>
        /// `AsSpan()` works similarly to `CollectionsMarshal.AsSpan()`.
        /// Note that adding or removing elements from a collection may reference discarded buffers.
        /// </summary>
        public static Span<T> AsSpan(ArrayPoolList<T> source)
        {
            if (source.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return source.m_Array.AsSpan(..source.m_Length);
        }

        public int BinarySearch(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return m_Array.AsSpan(..m_Length).BinarySearch(item, Comparer<T>.Default);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return m_Array.AsSpan(..m_Length).BinarySearch(item, comparer);
        }

        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array.AsSpan(..m_Length).Clear();
            }
            m_Length = 0;
            m_Version++;
        }

        public bool Contains(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return EquatableSpanHelper.IndexOf(m_Array.AsSpan(..m_Length), item) != -1;
        }

        public ArrayPoolList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var converted = new ArrayPoolList<TOutput>(m_Length);

            int i = 0;
            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                converted.m_Array![i] = converter(element);
                i++;
            }
            converted.m_Length = i;

            return converted;
        }

        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (destination is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(destination));
            }
            if (sourceIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(sourceIndex), 0, m_Length, sourceIndex);
            }
            if (destinationIndex + count >= destination.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length - sourceIndex, destinationIndex + count);
            }

            m_Array.AsSpan(sourceIndex..m_Length).CopyTo(destination.AsSpan(destinationIndex..(destinationIndex + count)));
        }

        public void CopyTo(T[] array)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (array is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(array));
            }

            m_Array.AsSpan(..m_Length).CopyTo(array.AsSpan());
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Array.AsSpan(..m_Length).CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(Span<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Array.AsSpan(..m_Length).CopyTo(span);
        }

        public int EnsureCapacity(int capacity)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            if (m_Array.Length < capacity)
            {
                Grow(capacity);
            }

            return m_Array.Length;
        }

        public bool Exists(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                if (predicate(element))
                {
                    return true;
                }
            }

            return false;
        }

        public T? Find(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                if (predicate(element))
                {
                    return element;
                }
            }

            return default;
        }

        public ArrayPoolList<T> FindAll(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var result = new ArrayPoolList<T>();

            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                if (predicate(element))
                {
                    result.Add(element);
                }
            }

            return result;
        }

        public int FindIndex(int startIndex, int count, Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex + count > m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length, startIndex + count);
            }

            int i = startIndex;
            foreach (var element in m_Array.AsSpan(startIndex..(startIndex + count)))
            {
                if (predicate(element))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public int FindIndex(int startIndex, Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex > m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, m_Length, startIndex);
            }

            int i = startIndex;
            foreach (var element in m_Array.AsSpan(startIndex..m_Length))
            {
                if (predicate(element))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public int FindIndex(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int i = 0;
            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                if (predicate(element))
                {
                    return i;
                }
                i++;
            }
            return -1;
        }

        public T? FindLast(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            for (int i = m_Length - 1; i >= 0; i--)
            {
                if (predicate(m_Array[i]))
                {
                    return m_Array[i];
                }
            }

            return default;
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) > m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, startIndex + count);
            }

            for (int i = startIndex + count - 1; i >= startIndex; i--)
            {
                if (predicate(m_Array[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindLastIndex(int startIndex, Predicate<T> predicate)
        {
            return FindLastIndex(startIndex, m_Length - startIndex, predicate);
        }

        public int FindLastIndex(Predicate<T> predicate)
        {
            return FindLastIndex(0, m_Length, predicate);
        }

        public void ForEach(Action<T> action)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int version = m_Version;

            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                action(element);

                if (m_Version != version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }
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

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public ArrayPoolList<T> GetRange(int startIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) > m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length, startIndex + count);
            }

            var result = new ArrayPoolList<T>(count);

            m_Array.AsSpan(startIndex..(startIndex + count)).CopyTo(result.m_Array.AsSpan());
            result.m_Length = count;

            return result;
        }

        public int IndexOf(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return EquatableSpanHelper.IndexOf(m_Array.AsSpan(..m_Length), item);
        }

        public int IndexOf(T item, int startIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }

            return EquatableSpanHelper.IndexOf(m_Array.AsSpan(startIndex..m_Length), item);
        }

        public int IndexOf(T item, int startIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) > m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, startIndex + count);
            }

            int index = EquatableSpanHelper.IndexOf(m_Array.AsSpan(startIndex..(startIndex + count)), item);
            return index == -1 ? -1 : (index + startIndex);
        }

        public void Insert(int index, T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)index > m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length + 1, index);
            }

            if (m_Length + 1 > m_Array.Length)
            {
                // TODO: optimizable(copy)
                Grow(m_Array.Length + 1);
            }

            m_Array.AsSpan(index..m_Length).CopyTo(m_Array.AsSpan((index + 1)..));
            m_Array[index] = item;
            m_Length++;
            m_Version++;
        }

        public void InsertRange(int index, IEnumerable<T> source)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)index > m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length + 1, index);
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                if (m_Length + count > m_Array.Length)
                {
                    // TODO: optimizable(copy)
                    Grow(m_Length + count);
                }

                m_Array.AsSpan(index..m_Length).CopyTo(m_Array.AsSpan((index + count)..));

                if (source is ICollection<T> genericCollection)
                {
                    genericCollection.CopyTo(m_Array, index);
                }
                else if (source is ICollection collection)
                {
                    collection.CopyTo(m_Array, index);
                }
                else
                {
                    int i = index;
                    foreach (var element in source)
                    {
                        m_Array[i++] = element;
                    }
                }
            }
            else
            {
                using var segmentedArray = new SegmentedArray<T>(SegmentedArray<T>.Stack16.Create().AsSpan());

                segmentedArray.AddRange(source);
                count = segmentedArray.GetTotalLength();
                if (m_Length + count > m_Array.Length)
                {
                    // TODO: optimizable(copy)
                    Grow(m_Length + count);
                }

                m_Array.AsSpan(index..m_Length).CopyTo(m_Array.AsSpan((index + count)..));

                segmentedArray.CopyTo(m_Array.AsSpan(index..));
            }

            m_Length += count;
            m_Version++;
        }

        public void InsertRangeFromSpan(int index, ReadOnlySpan<T> source)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)index > m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length + 1, index);
            }

            if (m_Length + source.Length > m_Array.Length)
            {
                // TODO: optimizable(copy)
                Grow(m_Length + source.Length);
            }

            m_Array.AsSpan(index..m_Length).CopyTo(m_Array.AsSpan((index + source.Length)..));
            source.CopyTo(m_Array.AsSpan(index..));

            m_Length += source.Length;
            m_Version++;
        }

        public int LastIndexOf(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return EquatableSpanHelper.LastIndexOf(m_Array.AsSpan(..m_Length), item);
        }

        public int LastIndexOf(T item, int startIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }

            int index = EquatableSpanHelper.LastIndexOf(m_Array.AsSpan(startIndex..m_Length), item);
            return index == -1 ? -1 : (startIndex + index);
        }

        public int LastIndexOf(T item, int startIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, startIndex + count);
            }

            int index = EquatableSpanHelper.LastIndexOf(m_Array.AsSpan(startIndex..(startIndex + count)), item);
            return index == -1 ? -1 : (startIndex + index);
        }

        public bool Remove(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int index = EquatableSpanHelper.IndexOf(m_Array.AsSpan(..m_Length), item);
            if (index == -1)
            {
                return false;
            }

            m_Array.AsSpan((index + 1)..m_Length).CopyTo(m_Array.AsSpan(index..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array[m_Length - 1] = default!;
            }
            m_Length--;
            m_Version++;
            return true;
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int skipCount = 0;

            for (int i = 0; i < m_Length; i++)
            {
                if (predicate(m_Array[i]))
                {
                    skipCount++;
                }
                else
                {
                    m_Array[i - skipCount] = m_Array[i];
                }
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array.AsSpan((m_Length - skipCount)..m_Length).Clear();
            }

            m_Length -= skipCount;
            m_Version++;
            return skipCount;
        }

        public void RemoveAt(int index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)index >= m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, index);
            }

            m_Array.AsSpan((index + 1)..m_Length).CopyTo(m_Array.AsSpan(index..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array[m_Length - 1] = default!;
            }
            m_Length--;
            m_Version++;
        }

        public void RemoveRange(int startIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length, startIndex + count);
            }

            m_Array.AsSpan((startIndex + count)..m_Length).CopyTo(m_Array.AsSpan(startIndex..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array.AsSpan((m_Length - count)..m_Length).Clear();
            }
            m_Length -= count;
            m_Version++;
        }

        public void Reverse()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            for (int i = 0; i < m_Length / 2; i++)
            {
                (m_Array[i], m_Array[m_Length - 1 - i]) = (m_Array[m_Length - 1 - i], m_Array[i]);
            }
            m_Version++;
        }

        public void Reverse(int startIndex, int count)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, count);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length, startIndex + count);
            }

            for (int i = 0; i < count / 2; i++)
            {
                (m_Array[startIndex + i], m_Array[startIndex + count - 1 - i]) = (m_Array[startIndex + count - 1 - i], m_Array[startIndex + i]);
            }
            m_Version++;
        }

        /// <summary>
        /// `SetCount()` works similarly as `CollectionsMarshal.SetCount()`.
        /// Use with caution as it may reference uninitialized area.
        /// </summary>
        public static void SetCount(ArrayPoolList<T> source, int count)
        {
            if (source.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, source.m_Array.Length, count);
            }
            if (count > source.m_Array.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, source.m_Array.Length, count);
            }

            source.m_Length = count;
            source.m_Version++;
        }

        public ArrayPoolList<T> Slice(int startIndex, int count)
        {
            return GetRange(startIndex, count);
        }

        public void Sort()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            Array.Sort(m_Array, 0, m_Length);
            m_Version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            // TODO: alloc?
            Array.Sort(m_Array, 0, m_Length, Comparer<T>.Create(comparison));
            m_Version++;
        }

        public void Sort(IComparer<T> comparer)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            Array.Sort(m_Array, 0, m_Length, comparer);
            m_Version++;
        }

        public void Sort(int startIndex, int count, IComparer<T> comparer)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (startIndex >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, m_Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, m_Length, count);
            }
            if ((uint)(startIndex + count) >= m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, m_Length, startIndex + count);
            }

            Array.Sort(m_Array, startIndex, count, comparer);
            m_Version++;
        }

        public T[] ToArray()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var result = CollectionHelper.AllocateUninitializedArray<T>(m_Length);
            m_Array.AsSpan(0..m_Length).CopyTo(result);
            return result;
        }

        public override string ToString()
        {
            return $"{m_Length} items";
        }

        public void TrimExcess()
        {
            Capacity = m_Length;
        }

        public bool TrueForAll(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            foreach (var element in m_Array.AsSpan(..m_Length))
            {
                if (!predicate(element))
                {
                    return false;
                }
            }
            return true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array);
                m_Array = null;
            }
            m_Length = 0;
            m_Version = int.MinValue;
        }

        int IList.Add(object? value)
        {
            if (value is T typedValue)
            {
                Add(typedValue);
                return m_Length - 1;
            }
            else if (value is null && default(T) is null)
            {
                Add(default!);
                return m_Length - 1;
            }

            ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            return -1;
        }

        bool IList.Contains(object? value)
        {
            if (value is T typedValue)
            {
                return Contains(typedValue);
            }
            else if (value is null && default(T) is null)
            {
                return Contains(default!);
            }

            return false;
        }

        int IList.IndexOf(object? value)
        {
            if (value is T typedValue)
            {
                return IndexOf(typedValue);
            }
            else if (value is null && default(T) is null)
            {
                return IndexOf(default!);
            }

            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            if (value is T typedValue)
            {
                Insert(index, typedValue);
            }
            else if (value is null && default(T) is null)
            {
                Insert(index, default!);
            }
            else
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            }
        }

        void IList.Remove(object? value)
        {
            if (value is T typedValue)
            {
                Remove(typedValue);
            }
            else if (value is null && default(T) is null)
            {
                Remove(default!);
            }
            else
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
            }
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
            if (array.GetType() != m_Array.GetType())
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }

            Array.Copy(m_Array, 0, array, index, m_Length);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayPoolList<T> Source;
            private int Index;
            private readonly int Version;

            internal Enumerator(ArrayPoolList<T> source)
            {
                Source = source;
                Index = -1;
                Version = source.m_Version;
            }

            public readonly T Current
            {
                get
                {
                    if (Source.m_Version != Version)
                    {
                        ThrowHelper.ThrowDifferentVersion();
                    }
                    if ((uint)Index >= Source.m_Length)
                    {
                        ThrowHelper.ThrowEnumeratorUndefined();
                    }

                    return Source[Index];
                }
            }

            readonly object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (Source.m_Version != Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }
                if (Index >= Source.m_Length)
                {
                    return false;
                }

                return ++Index < Source.m_Length;
            }

            public void Reset()
            {
                if (Source.m_Version != Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                Index = -1;
            }
        }
    }
}
