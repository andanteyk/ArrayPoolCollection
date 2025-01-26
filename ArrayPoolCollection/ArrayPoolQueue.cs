using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolQueue<T> : IReadOnlyCollection<T>, ICollection, IDisposable
    {
        private T[]? m_Array;
        private int m_Head;
        private int m_Length;
        private int m_Version;


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

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => this;

        public ArrayPoolQueue() : this(16) { }
        public ArrayPoolQueue(int capacity)
        {
            m_Array = ArrayPool<T>.Shared.Rent(capacity);
            m_Version = 0;
            m_Head = 0;
            m_Length = 0;
        }
        public ArrayPoolQueue(IEnumerable<T> source)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 16;
            }

            m_Array = ArrayPool<T>.Shared.Rent(Math.Max(count, 16));
            m_Version = 0;
            m_Head = 0;
            m_Length = 0;

            // TODO: opt @ icollection
            foreach (var element in source)
            {
                Enqueue(element);
            }
        }


        public void Clear()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            {
                m_Array.AsSpan().Clear();
            }

            m_Head = 0;
            m_Length = 0;
            m_Version++;
        }

        public bool Contains(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Head + m_Length <= m_Array.Length)
            {
                return EquatableSpanHelper.IndexOf(m_Array.AsSpan(m_Head..(m_Head + m_Length)), item) >= 0;
            }
            else
            {
                return EquatableSpanHelper.IndexOf(m_Array.AsSpan(m_Head..), item) >= 0 ||
                    EquatableSpanHelper.IndexOf(m_Array.AsSpan(..(m_Head + m_Length - m_Array.Length)), item) >= 0;
            }
        }

        public void CopyTo(T[] array, int index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, array.Length - Count, index);
            }
            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, array.Length - Count, index);
            }

            if (m_Head + m_Length <= m_Array.Length)
            {
                m_Array.AsSpan(m_Head..(m_Head + m_Length)).CopyTo(array.AsSpan(index..));
            }
            else
            {
                m_Array.AsSpan(m_Head..).CopyTo(array.AsSpan(index..));
                m_Array.AsSpan(..(m_Head + m_Length - m_Array.Length)).CopyTo(array.AsSpan((index + (m_Array.Length - m_Head))..));
            }
        }

        public void CopyTo(Span<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (span.Length < Count)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(span), 0, span.Length - Count, span.Length);
            }

            if (m_Head + m_Length <= m_Array.Length)
            {
                m_Array.AsSpan(m_Head..(m_Head + m_Length)).CopyTo(span);
            }
            else
            {
                m_Array.AsSpan(m_Head..).CopyTo(span);
                m_Array.AsSpan(..(m_Head + m_Length - m_Array.Length)).CopyTo(span[(m_Array.Length - m_Head)..]);
            }
        }

        public T Dequeue()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            int index = m_Head;
            if (++m_Head >= m_Array.Length)
            {
                m_Head -= m_Array.Length;
            }

            m_Length--;
            m_Version++;
            return m_Array[index];
        }

        public void Enqueue(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == m_Array.Length)
            {
                Resize(m_Array.Length << 1);
            }

            int index = m_Head + m_Length;
            if (index >= m_Array.Length)
            {
                index -= m_Array.Length;
            }

            m_Array[index] = item;
            m_Length++;
            m_Version++;
        }

        public void EnqueueRange(IEnumerable<T> items)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(items, out int count))
            {
                int from = m_Head + m_Length;
                if (from >= m_Array.Length)
                {
                    from -= m_Array.Length;
                }

                if (from + count > m_Array.Length - m_Length)
                {
                    Resize(from + count);
                }
            }

            if (items is ICollection<T> collection)
            {
                int from = m_Head + m_Length;
                if (from >= m_Array.Length)
                {
                    from -= m_Array.Length;
                }

                collection.CopyTo(m_Array, from);
                m_Length += count;
                m_Version++;
            }
            else
            {
                foreach (var item in items)
                {
                    Enqueue(item);
                }
            }
        }

        public void EnqueueRange(T[] items) => EnqueueRange(items.AsSpan());

        public void EnqueueRange(ReadOnlySpan<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length + span.Length > m_Array.Length)
            {
                Resize(m_Length + span.Length);
            }

            int from = m_Head + m_Length;
            if (from >= m_Array.Length)
            {
                from -= m_Array.Length;
            }

            if (from + span.Length < m_Array.Length)
            {
                span.CopyTo(m_Array.AsSpan(from..));
            }
            else
            {
                int rest = m_Array.Length - from;
                span[..rest].CopyTo(m_Array.AsSpan(from..));
                span[rest..].CopyTo(m_Array.AsSpan());
            }

            m_Length += span.Length;
            m_Version++;
        }

        private void Resize(int size)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var oldArray = m_Array;
            m_Array = ArrayPool<T>.Shared.Rent(Math.Max(size, 16));

            if (m_Head + m_Length <= oldArray.Length)
            {
                oldArray.AsSpan(m_Head..(m_Head + m_Length)).CopyTo(m_Array.AsSpan());
            }
            else
            {
                oldArray.AsSpan(m_Head..).CopyTo(m_Array.AsSpan());
                oldArray.AsSpan(..(m_Head + m_Length - oldArray.Length)).CopyTo(m_Array.AsSpan((oldArray.Length - m_Head)..));
            }

            ArrayPool<T>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            m_Head = 0;
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

            if (capacity < m_Array.Length)
            {
                return m_Array.Length;
            }

            Resize(capacity);
            return m_Array.Length;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayPoolQueue<T> m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(ArrayPoolQueue<T> parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            readonly public T Current
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

                    int index = m_Parent.m_Head + m_Index;
                    if (index >= m_Parent.m_Array.Length)
                    {
                        index -= m_Parent.m_Array.Length;
                    }
                    return m_Parent.m_Array[index];
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
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Parent.m_Array));
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
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return new Enumerator(this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public T Peek()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            return m_Array[m_Head];
        }

        public T[] ToArray()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var result = new T[Count];
            CopyTo(result);

            return result;
        }

        public override string ToString()
        {
            return $"{Count} items";
        }

        public void TrimExcess()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            TrimExcess(Count);
        }

        public void TrimExcess(int capacity)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (capacity < Count)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), Count, int.MaxValue, capacity);
            }

            Resize(capacity);
        }

        public bool TryDequeue(out T value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                value = default!;
                return false;
            }

            int index = m_Head;
            if (++m_Head >= m_Array.Length)
            {
                m_Head -= m_Array.Length;
            }

            m_Length--;
            m_Version++;
            value = m_Array[index];
            return true;
        }

        public bool TryPeek(out T value)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                value = default!;
                return false;
            }

            value = m_Array[m_Head];
            return true;
        }

        void ICollection.CopyTo(Array array, int index)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (index < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, array.Length - Count, index);
            }
            if (array.Length - index < Count)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, array.Length - Count, index);
            }

            if (m_Head + m_Length <= m_Array.Length)
            {
                Array.Copy(m_Array, m_Head, array, index, m_Length);
            }
            else
            {
                Array.Copy(m_Array, m_Head, array, index, m_Array.Length - m_Head);
                Array.Copy(m_Array, 0, array, index + m_Array.Length - m_Head, m_Head + m_Length - m_Array.Length);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Array = null;
            }
            m_Version = int.MinValue;
        }
    }
}