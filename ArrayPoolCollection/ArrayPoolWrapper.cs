using System.Buffers;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    [CollectionBuilder(typeof(ArrayPoolWrapperBuilder), nameof(ArrayPoolWrapperBuilder.Create))]
    public sealed class ArrayPoolWrapper<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable
    {
        private T[]? m_Array;
        private readonly int m_Length;

        public ArrayPoolWrapper(int length) : this(length, true) { }

        public ArrayPoolWrapper(int length, bool clearArray)
        {
            if (length < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, CollectionHelper.ArrayMaxLength, length);
            }

            m_Array = ArrayPool<T>.Shared.Rent(length);
            m_Length = length;

            if (clearArray)
            {
                m_Array.AsSpan(..m_Length).Clear();
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

        public int Length
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

        public long LongLength
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

        public int Rank => 1;

        int ICollection<T>.Count => Length;
        int IReadOnlyCollection<T>.Count => Length;

        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsFixedSize => true;

        bool IList.IsReadOnly => false;

        int ICollection.Count => Length;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        object? IList.this[int index]
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

                if (value is T typed)
                {
                    m_Array[index] = typed;
                }
                else if (value is null && default(T) is null)
                {
                    m_Array[index] = default!;
                }
                else
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(value));
                }
            }
        }


        public ReadOnlyCollection<T> AsReadOnly()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return new ReadOnlyCollection<T>(this);
        }

        public Span<T> AsSpan()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return m_Array.AsSpan(..m_Length);
        }

        public Span<T> AsSpan(Index index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int offset = index.GetOffset(m_Length);
            if ((uint)offset > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, offset);
            }

            return m_Array.AsSpan(offset..m_Length);
        }

        public Span<T> AsSpan(int startIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }

            return m_Array.AsSpan(startIndex..m_Length);
        }

        public Span<T> AsSpan(Range range)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)range.Start.GetOffset(m_Length) > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(range), 0, m_Length, range.Start.GetOffset(m_Length));
            }
            if ((uint)range.End.GetOffset(m_Length) > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(range), 0, m_Length, range.End.GetOffset(m_Length));
            }

            return m_Array.AsSpan(range);
        }

        public Span<T> AsSpan(int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            return m_Array.AsSpan(startIndex, length);
        }

        public static implicit operator Span<T>(ArrayPoolWrapper<T> source)
        {
            return source.AsSpan();
        }

        public Memory<T> AsMemory()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return m_Array.AsMemory(..m_Length);
        }

        public Memory<T> AsMemory(Index index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int offset = index.GetOffset(m_Length);
            if ((uint)offset > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, offset);
            }

            return m_Array.AsMemory(offset..m_Length);
        }

        public Memory<T> AsMemory(int startIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }

            return m_Array.AsMemory(startIndex..m_Length);
        }

        public Memory<T> AsMemory(Range range)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)range.Start.GetOffset(m_Length) > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(range), 0, m_Length, range.Start.GetOffset(m_Length));
            }
            if ((uint)range.End.GetOffset(m_Length) > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(range), 0, m_Length, range.End.GetOffset(m_Length));
            }

            return m_Array.AsMemory(range);
        }

        public Memory<T> AsMemory(int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            return m_Array.AsMemory(startIndex, length);
        }

        public int BinarySearch(T value) => BinarySearch(0, m_Length, value, null);
        public int BinarySearch(T value, IComparer<T>? comparer) => BinarySearch(0, m_Length, value, comparer);
        public int BinarySearch(int startIndex, int length, T value) => BinarySearch(startIndex, length, value, null);
        public int BinarySearch(int startIndex, int length, T value, IComparer<T>? comparer)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (comparer is null)
            {
                if (value is IComparable<T>)
                {
                    return Array.BinarySearch(m_Array, startIndex, length, value);
                }
                else
                {
                    ThrowHelper.ThrowDoesNotImplement<IComparable<T>>(nameof(value));
                }
            }

            return Array.BinarySearch(m_Array, startIndex, length, value, comparer);
        }

        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan(..m_Length).Clear();
        }

        public void Clear(int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan(startIndex..(startIndex + length)).Clear();
        }

        public ArrayPoolWrapper<T> Clone()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var clone = new ArrayPoolWrapper<T>(m_Length);
            CopyTo(clone);
            return clone;
        }

        public ArrayPoolWrapper<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var result = new ArrayPoolWrapper<TOutput>(m_Length);
            for (int i = m_Length - 1; i >= 0; i--)
            {
                result[i] = converter(this[i]);
            }

            return result;
        }

        internal void CopyFrom(ICollection<T> collection)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            collection.CopyTo(m_Array, 0);
        }

        public void CopyTo(Span<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan().CopyTo(span);
        }

        public void CopyTo(Memory<T> memory)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Array.AsMemory(..m_Length).CopyTo(memory);
        }

        public void CopyTo(T[] array)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan().CopyTo(array);
        }

        public void CopyTo(T[] array, int startIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            CopyTo(array.AsSpan(startIndex..));
        }

        public bool Exists(Predicate<T> predicate)
        {
            foreach (var element in AsSpan())
            {
                if (predicate(element))
                {
                    return true;
                }
            }

            return false;
        }

        public void Fill(T value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan().Fill(value);
        }

        public void Fill(T value, int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            AsSpan(startIndex..(startIndex + length)).Fill(value);
        }

        public T? Find(Predicate<T> predicate)
        {
            foreach (var element in AsSpan())
            {
                if (predicate(element))
                {
                    return element;
                }
            }

            return default;
        }

        public ArrayPoolWrapper<T> FindAll(Predicate<T> predicate)
        {
            using var segmentedArray = new SegmentedArray<T>(SegmentedArray<T>.Stack16.Create().AsSpan());

            foreach (var element in AsSpan())
            {
                if (predicate(element))
                {
                    segmentedArray.Add(element);
                }
            }

            var result = new ArrayPoolWrapper<T>(segmentedArray.GetTotalLength());
            segmentedArray.CopyTo(result);
            return result;
        }

        public int FindIndex(Predicate<T> predicate) => FindIndex(0, m_Length, predicate);
        public int FindIndex(int startIndex, Predicate<T> predicate) => FindIndex(startIndex, m_Length - startIndex, predicate);
        public int FindIndex(int startIndex, int length, Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            int i = startIndex;
            foreach (var element in AsSpan(startIndex..(startIndex + length)))
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

        public int FindLastIndex(Predicate<T> predicate) => FindLastIndex(0, m_Length, predicate);
        public int FindLastIndex(int startIndex, Predicate<T> predicate) => FindLastIndex(startIndex, m_Length - startIndex, predicate);
        public int FindLastIndex(int startIndex, int length, Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            for (int i = startIndex + length - 1; i >= startIndex; i--)
            {
                if (predicate(m_Array[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            foreach (var element in AsSpan())
            {
                action(element);
            }
        }

        public void ForEach(Action<T, int> action)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int i = 0;
            foreach (var element in AsSpan())
            {
                action(element, i);
                i++;
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

        public int IndexOf(T value) => IndexOf(value, 0, m_Length);
        public int IndexOf(T value, int startIndex) => IndexOf(value, startIndex, m_Length - startIndex);
        public int IndexOf(T value, int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            return EquatableSpanHelper.IndexOf(AsSpan(startIndex..(startIndex + length)), value);
        }

        public int LastIndexOf(T value) => LastIndexOf(value, 0, m_Length);
        public int LastIndexOf(T value, int startIndex) => LastIndexOf(value, startIndex, m_Length - startIndex);
        public int LastIndexOf(T value, int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            return EquatableSpanHelper.LastIndexOf(AsSpan(startIndex..(startIndex + length)), value);
        }

        public static void Resize(ref ArrayPoolWrapper<T> array, int newSize)
        {
            if (array.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)newSize > (uint)CollectionHelper.ArrayMaxLength)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(newSize), 0, CollectionHelper.ArrayMaxLength, newSize);
            }

            var newArray = new ArrayPoolWrapper<T>(newSize);
            array.AsSpan(..Math.Min(array.m_Length, newSize)).CopyTo(newArray);

            array.Dispose();
            array = newArray;
        }

        public void Reverse()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            AsSpan().Reverse();
        }
        public void Reverse(int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            AsSpan(startIndex..(startIndex + length)).Reverse();
        }

        public Span<T> Slice(int startIndex, int length)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            return AsSpan(startIndex..(startIndex + length));
        }

        public void Sort() => Sort(0, m_Length, null);
        public void Sort(Comparison<T> comparison) => Sort(0, m_Length, Comparer<T>.Create(comparison));
        public void Sort(IComparer<T>? comparer) => Sort(0, m_Length, comparer);
        public void Sort(int startIndex, int length) => Sort(startIndex, length, null);
        public void Sort(int startIndex, int length, IComparer<T>? comparer)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)startIndex > (uint)m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, m_Length, startIndex);
            }
            if ((uint)length > (uint)(m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, m_Length - startIndex, length);
            }

            Array.Sort(m_Array, startIndex, length, comparer);
        }

        public static void Sort<TValue>(ArrayPoolWrapper<T> keys, ArrayPoolWrapper<TValue>? values) => Sort(keys, values, 0, keys.m_Length, null);
        public static void Sort<TValue>(ArrayPoolWrapper<T> keys, ArrayPoolWrapper<TValue>? values, IComparer<T> comparer) => Sort(keys, values, 0, keys.m_Length, comparer);
        public static void Sort<TValue>(ArrayPoolWrapper<T> keys, ArrayPoolWrapper<TValue>? values, int startIndex, int length) => Sort(keys, values, startIndex, length, null);
        public static void Sort<TValue>(ArrayPoolWrapper<T> keys, ArrayPoolWrapper<TValue>? values, int startIndex, int length, IComparer<T>? comparer)
        {
            if (keys.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(keys.m_Array));
            }
            if (values is not null && values.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(values.m_Array));
            }
            if ((uint)startIndex > (uint)keys.m_Length || (uint)startIndex > (uint?)values?.m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(startIndex), 0, Math.Min(keys.m_Length, values?.m_Length ?? int.MaxValue), startIndex);
            }
            if ((uint)length > (uint)(keys.m_Length - startIndex) || (uint)length > (uint?)(values?.m_Length - startIndex))
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(length), 0, Math.Min(keys.m_Length, values?.m_Length ?? int.MaxValue) - startIndex, length);
            }

            Array.Sort(keys.m_Array, values?.m_Array, startIndex, length, comparer);
        }

        public override string ToString()
        {
            return $"{Length} items";
        }

        public bool TrueForAll(Predicate<T> predicate)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            foreach (var element in AsSpan())
            {
                if (!predicate(element))
                {
                    return false;
                }
            }
            return true;
        }


        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Array = null;
            }
        }


        void ICollection<T>.Add(T item)
        {
            throw new NotSupportedException();
        }

        void ICollection<T>.Clear()
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Contains(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return EquatableSpanHelper.IndexOf(AsSpan(), item) != -1;
        }

        void ICollection<T>.CopyTo(T[] array, int arrayIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if ((uint)arrayIndex >= array.Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(arrayIndex), 0, array.Length, arrayIndex);
            }

            AsSpan().CopyTo(array.AsSpan(arrayIndex..));
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        int IList<T>.IndexOf(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return EquatableSpanHelper.IndexOf(AsSpan(), item);
        }

        void IList<T>.Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        bool ICollection<T>.Remove(T item)
        {
            throw new NotSupportedException();
        }

        void IList<T>.RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        int IList.Add(object? value)
        {
            throw new NotSupportedException();
        }

        void IList.Clear()
        {
            throw new NotSupportedException();
        }

        bool IList.Contains(object? value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (value is T typedValue)
            {
                return ((ICollection<T>)this).Contains(typedValue);
            }
            else if (value is null && default(T) is null)
            {
                return ((ICollection<T>)this).Contains(default!);
            }

            return false;
        }

        int IList.IndexOf(object? value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (value is T typedValue)
            {
                return ((IList<T>)this).IndexOf(typedValue);
            }
            else if (value is null && default(T) is null)
            {
                return ((IList<T>)this).IndexOf(default!);
            }

            return -1;
        }

        void IList.Insert(int index, object? value)
        {
            throw new NotSupportedException();
        }

        void IList.Remove(object? value)
        {
            throw new NotSupportedException();
        }

        void IList.RemoveAt(int index)
        {
            throw new NotSupportedException();
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
            private readonly ArrayPoolWrapper<T> m_Parent;
            private int m_Index;

            internal Enumerator(ArrayPoolWrapper<T> source)
            {
                m_Parent = source;
                m_Index = -1;
            }

            public readonly T Current
            {
                get
                {
                    if (m_Parent.m_Array is null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                    }
                    if ((uint)m_Index >= m_Parent.m_Length)
                    {
                        throw new InvalidOperationException();
                    }

                    return m_Parent[m_Index];
                }
            }

            readonly object? IEnumerator.Current => Current;

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                if (m_Parent.m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
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
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }

                m_Index = -1;
            }
        }
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ArrayPoolWrapperBuilder
    {
        public static ArrayPoolWrapper<T> Create<T>(ReadOnlySpan<T> source)
        {
            var result = new ArrayPoolWrapper<T>(source.Length, false);
            source.CopyTo(result);
            return result;
        }
    }
}
