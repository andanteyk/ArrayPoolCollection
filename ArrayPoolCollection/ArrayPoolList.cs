using System.Buffers;
using System.Collections;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolList<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable
    {
        private T[]? Array;
        private int Length;
        private int Version;

        public ArrayPoolList()
        {
            Array = ArrayPool<T>.Shared.Rent(16);
            Length = 0;
            Version = 0;
        }

        public ArrayPoolList(int capacity)
        {
            Array = ArrayPool<T>.Shared.Rent(capacity);
            Length = 0;
            Version = 0;
        }

        public ArrayPoolList(IEnumerable<T> source)
        {
            // TODO: may be able to optimize if source is ICollection(<T>)
            var segmentedStack = new SegmentedArray<T>.Stack16();
            var segmentedArray = new SegmentedArray<T>(segmentedStack.AsSpan());

            segmentedArray.AddRange(source);
            Array = segmentedArray.ToArrayPool(out var span);
            Length = span.Length;
            Version = 0;
        }

        public int Capacity
        {
            get
            {
                if (Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                return Array.Length;
            }
            set
            {
                if (Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                if (value < Length)
                {
                    ThrowHelper.ThrowArgumentOutOfRange(nameof(value), Length, 0x7FFFFFC7, value);
                }

                int newLength = Math.Max(CollectionHelper.RoundUpToPowerOf2(value), 16);
                if (newLength != Array.Length)
                {
                    var newArray = ArrayPool<T>.Shared.Rent(newLength);
                    Array.AsSpan(..Length).CopyTo(newArray);
                    ArrayPool<T>.Shared.Return(Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                    Array = newArray;
                    Version++;
                }
            }
        }


        public T this[int index]
        {
            get
            {
                if (Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                if ((uint)index >= Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(Length, index);
                }

                return Array[index];
            }
            set
            {
                if (Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                if ((uint)index >= Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(Length, index);
                }

                Array[index] = value;
            }
        }

        public int Count
        {
            get
            {
                if (Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                return Length;
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
            Array.AsSpan().CopyTo(newArray);
            ArrayPool<T>.Shared.Return(Array!, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            Array = newArray;

            Version++;
        }

        public void Add(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            if (Length + 1 > Array.Length)
            {
                Grow(Array.Length + 1);
            }
            Array[Length++] = item;
            Version++;
        }

        public void AddRange(IEnumerable<T> source)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                if (Length + count > Array.Length)
                {
                    Grow(Length + count);
                }

                if (source is ICollection<T> genericCollection)
                {
                    genericCollection.CopyTo(Array, Length);
                    Length += count;
                }
                else if (source is ICollection collection)
                {
                    collection.CopyTo(Array, Length);
                    Length += count;
                }
                else
                {
                    foreach (var element in source)
                    {
                        Array[Length++] = element;
                    }
                }
            }
            else
            {
                foreach (var element in source)
                {
                    if (Length + 1 > Array.Length)
                    {
                        Grow(Array.Length + 1);
                    }
                    Array[Length++] = element;
                }
            }

            Version++;
        }

        public void AddRangeFromSpan(ReadOnlySpan<T> source)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            if (Length + source.Length > Array.Length)
            {
                Grow(Length + source.Length);
            }
            source.CopyTo(Array.AsSpan(Length..));
            Length += source.Length;
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return new ReadOnlyCollection<T>(this);
        }

        public int BinarySearch(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return Array.AsSpan(..Length).BinarySearch(item, Comparer<T>.Default);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return Array.AsSpan(..Length).BinarySearch(item, comparer);
        }

        public void Clear()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.AsSpan(..Length).Clear();
            }
            Length = 0;
            Version++;
        }

        public bool Contains(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return EquatableSpanHelper.IndexOf(Array.AsSpan(..Length), item) != -1;
        }

        public ArrayPoolList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            var converted = new ArrayPoolList<TOutput>(Length);

            int i = 0;
            foreach (var element in Array.AsSpan(..Length))
            {
                converted.Array![i] = converter(element);
                i++;
            }
            converted.Length = i;

            return converted;
        }

        public void CopyTo(int sourceIndex, T[] destination, int destinationIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (destination is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(destination));
            }
            if (sourceIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(sourceIndex), 0, Length, sourceIndex);
            }
            if (destinationIndex + count >= destination.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length - sourceIndex, destinationIndex + count);
            }

            Array.AsSpan(sourceIndex..Length).CopyTo(destination.AsSpan(destinationIndex..(destinationIndex + count)));
        }

        public void CopyTo(T[] array)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (array is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(array));
            }

            Array.AsSpan(..Length).CopyTo(array.AsSpan());
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            Array.AsSpan(..Length).CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(Span<T> span)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            Array.AsSpan(..Length).CopyTo(span);
        }

        public int EnsureCapacity(int capacity)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            if (Array.Length < capacity)
            {
                Grow(capacity);
            }

            return Array.Length;
        }

        public bool Exists(Predicate<T> predicate)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            foreach (var element in Array.AsSpan(..Length))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            foreach (var element in Array.AsSpan(..Length))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            var result = new ArrayPoolList<T>();

            foreach (var element in Array.AsSpan(..Length))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex + count > Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length, startIndex + count);
            }

            int i = startIndex;
            foreach (var element in Array.AsSpan(startIndex..(startIndex + count)))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex > Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, Length, startIndex);
            }

            int i = startIndex;
            foreach (var element in Array.AsSpan(startIndex..Length))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            int i = 0;
            foreach (var element in Array.AsSpan(..Length))
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            for (int i = Length - 1; i >= 0; i--)
            {
                if (predicate(Array[i]))
                {
                    return Array[i];
                }
            }

            return default;
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> predicate)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, startIndex + count);
            }

            for (int i = startIndex + count - 1; i >= startIndex; i--)
            {
                if (predicate(Array[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public int FindLastIndex(int startIndex, Predicate<T> predicate)
        {
            return FindLastIndex(startIndex, Length - startIndex, predicate);
        }

        public int FindLastIndex(Predicate<T> predicate)
        {
            return FindLastIndex(0, Length, predicate);
        }

        public void ForEach(Action<T> action)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            int version = Version;

            foreach (var element in Array.AsSpan(..Length))
            {
                action(element);

                if (Version != version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }
            }
        }

        public Enumerator GetEnumerator()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        public ArrayPoolList<T> GetRange(int startIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) > Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length, startIndex + count);
            }

            var result = new ArrayPoolList<T>(count);

            Array.AsSpan(startIndex..(startIndex + count)).CopyTo(result.Array.AsSpan());
            result.Length = count;

            return result;
        }

        public int IndexOf(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return EquatableSpanHelper.IndexOf(Array.AsSpan(..Length), item);
        }

        public int IndexOf(T item, int startIndex)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }

            return EquatableSpanHelper.IndexOf(Array.AsSpan(startIndex..Length), item);
        }

        public int IndexOf(T item, int startIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, startIndex + count);
            }

            int index = EquatableSpanHelper.IndexOf(Array.AsSpan(startIndex..(startIndex + count)), item);
            return index == -1 ? -1 : (index + startIndex);
        }

        public void Insert(int index, T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)index > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, Length + 1, index);
            }

            if (Length + 1 > Array.Length)
            {
                // TODO: optimizable(copy)
                Grow(Array.Length + 1);
            }

            Array.AsSpan(index..Length).CopyTo(Array.AsSpan((index + 1)..));
            Array[index] = item;
            Length++;
            Version++;
        }

        public void InsertRange(int index, IEnumerable<T> source)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)index > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, Length + 1, index);
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                if (Length + count > Array.Length)
                {
                    // TODO: optimizable(copy)
                    Grow(Length + count);
                }

                Array.AsSpan(index..Length).CopyTo(Array.AsSpan((index + count)..));

                if (source is ICollection<T> genericCollection)
                {
                    genericCollection.CopyTo(Array, index);
                }
                else if (source is ICollection collection)
                {
                    collection.CopyTo(Array, index);
                }
                else
                {
                    int i = index;
                    foreach (var element in source)
                    {
                        Array[i++] = element;
                    }
                }
            }
            else
            {
                var segmentedStack = new SegmentedArray<T>.Stack16();
                using var segmentedArray = new SegmentedArray<T>(segmentedStack.AsSpan());

                segmentedArray.AddRange(source);
                count = segmentedArray.GetTotalLength();
                if (Length + count > Array.Length)
                {
                    // TODO: optimizable(copy)
                    Grow(Length + count);
                }

                Array.AsSpan(index..Length).CopyTo(Array.AsSpan((index + count)..));

                segmentedArray.CopyTo(Array.AsSpan(index..));
            }

            Length += count;
            Version++;
        }

        public void InsertRangeFromSpan(int index, ReadOnlySpan<T> source)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)index > Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, Length + 1, index);
            }

            if (Length + source.Length > Array.Length)
            {
                // TODO: optimizable(copy)
                Grow(Length + source.Length);
            }

            Array.AsSpan(index..Length).CopyTo(Array.AsSpan((index + source.Length)..));
            source.CopyTo(Array.AsSpan(index..));

            Length += source.Length;
            Version++;
        }

        public int LastIndexOf(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return EquatableSpanHelper.LastIndexOf(Array.AsSpan(..Length), item);
        }

        public int LastIndexOf(T item, int startIndex)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }

            int index = EquatableSpanHelper.LastIndexOf(Array.AsSpan(startIndex..Length), item);
            return index == -1 ? -1 : (startIndex + index);
        }

        public int LastIndexOf(T item, int startIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, startIndex + count);
            }

            int index = EquatableSpanHelper.LastIndexOf(Array.AsSpan(startIndex..(startIndex + count)), item);
            return index == -1 ? -1 : (startIndex + index);
        }

        public bool Remove(T item)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            int index = EquatableSpanHelper.IndexOf(Array.AsSpan(..Length), item);
            if (index == -1)
            {
                return false;
            }

            Array.AsSpan((index + 1)..Length).CopyTo(Array.AsSpan(index..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array[Length - 1] = default!;
            }
            Length--;
            Version++;
            return true;
        }

        public int RemoveAll(Predicate<T> predicate)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            int skipCount = 0;

            for (int i = 0; i < Length; i++)
            {
                if (predicate(Array[i]))
                {
                    skipCount++;
                }
                else
                {
                    Array[i - skipCount] = Array[i];
                }
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.AsSpan((Length - skipCount)..Length).Clear();
            }

            Length -= skipCount;
            Version++;
            return skipCount;
        }

        public void RemoveAt(int index)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if ((uint)index >= Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, Length, index);
            }

            Array.AsSpan((index + 1)..Length).CopyTo(Array.AsSpan(index..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array[Length - 1] = default!;
            }
            Length--;
            Version++;
        }

        public void RemoveRange(int startIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length, startIndex + count);
            }

            Array.AsSpan((startIndex + count)..Length).CopyTo(Array.AsSpan(startIndex..));
            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                Array.AsSpan((Length - count)..Length).Clear();
            }
            Length -= count;
            Version++;
        }

        public void Reverse()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            for (int i = 0; i < Length / 2; i++)
            {
                (Array[i], Array[Length - 1 - i]) = (Array[Length - 1 - i], Array[i]);
            }
            Version++;
        }

        public void Reverse(int startIndex, int count)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, Length, startIndex);
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, count);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length, startIndex + count);
            }

            for (int i = 0; i < count / 2; i++)
            {
                (Array[startIndex + i], Array[startIndex + count - 1 - i]) = (Array[startIndex + count - 1 - i], Array[startIndex + i]);
            }
            Version++;
        }

        public ArrayPoolList<T> Slice(int startIndex, int count)
        {
            return GetRange(startIndex, count);
        }

        public void Sort()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            System.Array.Sort(Array, 0, Length);
            Version++;
        }

        public void Sort(Comparison<T> comparison)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            // TODO: alloc?
            System.Array.Sort(Array, 0, Length, Comparer<T>.Create(comparison));
            Version++;
        }

        public void Sort(IComparer<T> comparer)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            System.Array.Sort(Array, 0, Length, comparer);
            Version++;
        }

        public void Sort(int startIndex, int count, IComparer<T> comparer)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (startIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Length, startIndex);
            }
            if (startIndex >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(startIndex), 0, Length, startIndex);
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, Length, count);
            }
            if ((uint)(startIndex + count) >= Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, Length, startIndex + count);
            }

            System.Array.Sort(Array, startIndex, count, comparer);
            Version++;
        }

        public T[] ToArray()
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            var result = CollectionHelper.AllocateUninitializedArray<T>(Length);
            Array.AsSpan(0..Length).CopyTo(result);
            return result;
        }

        public override string ToString()
        {
            return $"{Length} items";
        }

        public void TrimExcess()
        {
            Capacity = Length;
        }

        public bool TrueForAll(Predicate<T> predicate)
        {
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            foreach (var element in Array.AsSpan(..Length))
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
            if (Array is not null)
            {
                ArrayPool<T>.Shared.Return(Array);
                Array = null;
            }
            Length = 0;
            Version = int.MinValue;
        }

        int IList.Add(object? value)
        {
            if (value is T typedValue)
            {
                Add(typedValue);
                return Length - 1;
            }
            else if (value is null && default(T) is null)
            {
                Add(default!);
                return Length - 1;
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
            if (Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }
            if (array.Rank > 1)
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }
            if (array.GetType() != Array.GetType())
            {
                ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
            }

            System.Array.Copy(Array, 0, array, index, Length);
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
                Version = source.Version;
            }

            public readonly T Current
            {
                get
                {
                    if (Source.Version != Version)
                    {
                        ThrowHelper.ThrowDifferentVersion();
                    }
                    if ((uint)Index >= Source.Length)
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
                if (Source.Version != Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }
                if (Index >= Source.Length)
                {
                    return false;
                }

                return ++Index < Source.Length;
            }

            public void Reset()
            {
                if (Source.Version != Version)
                {
                    ThrowHelper.ThrowDifferentVersion();
                }

                Index = -1;
            }
        }
    }
}
