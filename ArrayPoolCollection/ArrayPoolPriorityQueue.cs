using System.Collections;
using System.Runtime.CompilerServices;
using ArrayPoolCollection.Pool;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolPriorityQueue<TElement, TPriority> : IDisposable
    {
        private (TElement Element, TPriority Priority)[]? m_Array;
        private readonly IComparer<TPriority>? m_Comparer;
        private int m_Length;
        private int m_Version;

        public IComparer<TPriority> Comparer
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }

                if (typeof(TPriority).IsValueType)
                {
                    return m_Comparer ?? Comparer<TPriority>.Default;
                }
                else
                {
                    return m_Comparer!;
                }
            }
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

        public readonly struct UnorderedItemsCollection : IReadOnlyCollection<(TElement Element, TPriority Priority)>, ICollection
        {
            private readonly ArrayPoolPriorityQueue<TElement, TPriority> m_Parent;

            internal UnorderedItemsCollection(ArrayPoolPriorityQueue<TElement, TPriority> parent)
            {
                m_Parent = parent;
            }

            public int Count
            {
                get
                {
                    if (m_Parent.m_Array is null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
                    }

                    return m_Parent.m_Length;
                }
            }

            bool ICollection.IsSynchronized => false;

            object ICollection.SyncRoot => m_Parent;

            public struct Enumerator : IEnumerator<(TElement Element, TPriority Priority)>
            {
                private readonly ArrayPoolPriorityQueue<TElement, TPriority> m_Parent;
                private readonly int m_Version;
                private int m_Index;

                internal Enumerator(ArrayPoolPriorityQueue<TElement, TPriority> parent)
                {
                    m_Parent = parent;
                    m_Version = parent.m_Version;
                    m_Index = -1;
                }

                public readonly (TElement Element, TPriority Priority) Current
                {
                    get
                    {
                        if (m_Parent.m_Array is null)
                        {
                            ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
                        }
                        if (m_Version != m_Parent.m_Version)
                        {
                            ThrowHelper.ThrowDifferentVersion();
                        }
                        if ((uint)m_Index >= m_Parent.m_Length)
                        {
                            ThrowHelper.ThrowEnumeratorUndefined();
                        }

                        return m_Parent.m_Array[m_Index];
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
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
                    }
                    if (m_Version != m_Parent.m_Version)
                    {
                        ThrowHelper.ThrowDifferentVersion();
                    }
                    if (m_Index >= m_Parent.m_Length)
                    {
                        ThrowHelper.ThrowEnumeratorUndefined();
                    }

                    return ++m_Index < m_Parent.m_Length;
                }

                public void Reset()
                {
                    if (m_Parent.m_Array is null)
                    {
                        ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
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
                if (m_Parent.m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
                }

                return new Enumerator(m_Parent);
            }

            IEnumerator<(TElement Element, TPriority Priority)> IEnumerable<(TElement Element, TPriority Priority)>.GetEnumerator()
            {
                return GetEnumerator();
            }

            void ICollection.CopyTo(Array array, int index)
            {
                if (m_Parent.m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent));
                }
                if (array.Length - index < m_Parent.m_Length)
                {
                    ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Parent.m_Length - array.Length, index);
                }
                if (array.Rank > 1)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                if (array is not (TElement Element, TPriority Priority)[] typedArray)
                {
                    ThrowHelper.ThrowArgumentTypeMismatch(nameof(array));
                }
                else
                {
                    Array.Copy(m_Parent.m_Array, 0, array, index, m_Parent.m_Length);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public UnorderedItemsCollection UnorderedItems
        {
            get
            {
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }

                return new UnorderedItemsCollection(this);
            }
        }

        public ArrayPoolPriorityQueue() : this(16) { }
        public ArrayPoolPriorityQueue(IComparer<TPriority>? comparer) : this(16, comparer) { }
        public ArrayPoolPriorityQueue(int capacity) : this(capacity, typeof(TPriority).IsValueType ? null : Comparer<TPriority>.Default) { }
        public ArrayPoolPriorityQueue(int capacity, IComparer<TPriority>? comparer)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            m_Comparer = comparer;
            m_Length = 0;
            m_Version = 0;

            Resize(Math.Max(capacity, 16));
        }

        public ArrayPoolPriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> source) : this(source, typeof(TPriority).IsValueType ? null : Comparer<TPriority>.Default) { }
        public ArrayPoolPriorityQueue(IEnumerable<(TElement Element, TPriority Priority)> source, IComparer<TPriority>? comparer)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 16;
            }

            m_Comparer = comparer;
            m_Length = 0;
            m_Version = 0;

            Resize(Math.Max(count, 16));

            EnqueueRange(source);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Compare(TPriority left, TPriority right)
        {
            if (typeof(TPriority).IsValueType)
            {
                if (m_Comparer is null)
                {
                    return Comparer<TPriority>.Default.Compare(left, right);
                }
                else
                {
                    return m_Comparer.Compare(left, right);
                }
            }
            else
            {
                return m_Comparer!.Compare(left, right);
            }
        }

        /// <summary>
        /// `AsSpan()` works similarly to `CollectionsMarshal.AsSpan()`.
        /// Note that adding or removing elements from a collection may reference discarded buffers.
        /// </summary>
        public static Span<(TElement Element, TPriority Priority)> AsSpan(ArrayPoolPriorityQueue<TElement, TPriority> queue)
        {
            if (queue.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return queue.m_Array.AsSpan(..queue.m_Length);
        }

        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<(TElement Element, TPriority Priority)>())
            {
                m_Array.AsSpan(..m_Length).Clear();
            }

            m_Length = 0;
            m_Version++;
        }

        public TElement Dequeue()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            var result = m_Array[0];
            m_Array[0] = m_Array[--m_Length];

            ShiftDown(0);

            m_Version++;
            return result.Element;
        }

        private void ShiftDown(int i)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            while (true)
            {
                int leftChild = 2 * i + 1;
                int rightChild = 2 * i + 2;

                if (leftChild >= m_Length)
                {
                    break;
                }

                int greaterChild = rightChild >= m_Length ?
                    leftChild :
                    Compare(m_Array[leftChild].Priority, m_Array[rightChild].Priority) > 0 ? rightChild : leftChild;

                if (Compare(m_Array[i].Priority, m_Array[greaterChild].Priority) > 0)
                {
                    (m_Array[i], m_Array[greaterChild]) = (m_Array[greaterChild], m_Array[i]);
                }
                else
                {
                    break;
                }

                i = greaterChild;
            }
        }

        public TElement DequeueEnqueue(TElement element, TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            var result = m_Array[0];
            m_Array[0] = (element, priority);

            ShiftDown(0);

            m_Version++;
            return result.Element;
        }

        public void Enqueue(TElement element, TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length >= m_Array.Length)
            {
                Resize(m_Array.Length << 1);
            }

            m_Array[m_Length] = (element, priority);

            ShiftUp();

            m_Length++;
            m_Version++;
        }

        private void ShiftUp()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            int i = m_Length;
            while (i != 0)
            {
                int parent = (i - 1) / 2;
                if (Compare(m_Array[i].Priority, m_Array[parent].Priority) < 0)
                {
                    (m_Array[i], m_Array[parent]) = (m_Array[parent], m_Array[i]);
                }
                else
                {
                    break;
                }
                i = parent;
            }
        }

        private void Resize(int newCapacity)
        {
            newCapacity = CollectionHelper.RoundUpToPowerOf2(newCapacity);
            if (newCapacity <= 0)
            {
                newCapacity = CollectionHelper.ArrayMaxLength;
            }
            if (newCapacity == m_Array?.Length)
            {
                return;
            }

            var oldArray = m_Array;
            m_Array = SlimArrayPool<(TElement Element, TPriority Priority)>.Shared.Rent(Math.Max(newCapacity, 16));
            oldArray?.AsSpan(..m_Length).CopyTo(m_Array);

            if (oldArray is not null)
            {
                SlimArrayPool<(TElement Element, TPriority Priority)>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<(TElement Element, TPriority Priority)>());
            }

            m_Version++;
        }

        public TElement EnqueueDequeue(TElement element, TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                return element;
            }

            var result = m_Array[0];
            m_Array[0] = (element, priority);

            ShiftDown(0);

            m_Version++;
            return result.Element;
        }

        public void EnqueueRange(IEnumerable<TElement> elements, TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {

                if (CollectionHelper.TryGetNonEnumeratedCount(elements, out int count))
                {
                    if (count > m_Array.Length)
                    {
                        Resize(count);
                    }
                }

                int i = 0;
                foreach (var element in elements)
                {
                    if (i >= m_Array.Length)
                    {
                        m_Length = i;
                        Resize(m_Array.Length << 1);
                    }

                    m_Array[i++] = (element, priority);
                }
                m_Length = i;

                Heapify();
            }
            else
            {
                if (CollectionHelper.TryGetNonEnumeratedCount(elements, out int count))
                {
                    if (m_Length + count > m_Array.Length)
                    {
                        Resize(m_Length + count);
                    }
                }

                foreach (var element in elements)
                {
                    Enqueue(element, priority);
                }
            }
        }

        public void EnqueueRange(IEnumerable<(TElement Element, TPriority Priority)> elements)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                if (elements is ICollection<(TElement, TPriority)> collection)
                {
                    if (collection.Count > m_Array.Length)
                    {
                        Resize(collection.Count);
                    }

                    collection.CopyTo(m_Array, 0);
                    m_Length = collection.Count;
                }
                else
                {
                    if (CollectionHelper.TryGetNonEnumeratedCount(elements, out int count))
                    {
                        if (count > m_Array.Length)
                        {
                            Resize(count);
                        }
                    }

                    int i = 0;
                    foreach (var element in elements)
                    {
                        if (i >= m_Array.Length)
                        {
                            m_Length = i;
                            Resize(m_Array.Length << 1);
                        }

                        m_Array[i++] = element;
                    }
                    m_Length = i;
                }

                Heapify();
            }
            else
            {
                if (CollectionHelper.TryGetNonEnumeratedCount(elements, out int count))
                {
                    if (m_Length + count > m_Array.Length)
                    {
                        Resize(m_Length + count);
                    }
                }

                foreach (var (Element, Priority) in elements)
                {
                    Enqueue(Element, Priority);
                }
            }
        }

        private void Heapify()
        {
            for (int i = (m_Length - 1) / 2; i >= 0; i--)
            {
                ShiftDown(i);
            }
            m_Version++;
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

            if (m_Array.Length >= capacity)
            {
                return m_Array.Length;
            }

            Resize(Math.Max(capacity, 16));
            return m_Array.Length;
        }

        public TElement Peek()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            return m_Array[0].Element;
        }

        /// <summary>
        /// `SetCount()` works similarly as `CollectionsMarshal.SetCount()`.
        /// Use with caution as it may reference uninitialized area.
        /// </summary>
        public static void SetCount(ArrayPoolPriorityQueue<TElement, TPriority> queue, int count)
        {
            if (queue.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, queue.m_Array.Length, count);
            }
            if (count > queue.m_Array.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, queue.m_Array.Length, count);
            }

            queue.m_Length = count;
            queue.m_Version++;
        }

        public override string ToString()
        {
            return $"{m_Length} items";
        }

        public void TrimExcess()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length < m_Array.Length / 2)
            {
                Resize(Math.Max(m_Length, 16));
            }
        }

        public bool TryDequeue(out TElement element, out TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            var result = m_Array[0];
            m_Array[0] = m_Array[--m_Length];

            ShiftDown(0);

            m_Version++;
            (element, priority) = result;
            return true;
        }

        public bool TryPeek(out TElement element, out TPriority priority)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                element = default!;
                priority = default!;
                return false;
            }

            (element, priority) = m_Array[0];
            return true;
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                SlimArrayPool<(TElement, TPriority)>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<(TElement, TPriority)>());
                m_Array = null;
            }

            m_Length = 0;
            m_Version = int.MinValue;
        }
    }
}
