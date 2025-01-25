using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolStack<T> : IReadOnlyCollection<T>, ICollection, IDisposable
    {
        private T[]? m_Array;
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



        public ArrayPoolStack() : this(16) { }

        public ArrayPoolStack(int capacity)
        {
            if (capacity < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), 0, int.MaxValue, capacity);
            }

            m_Array = ArrayPool<T>.Shared.Rent(Math.Max(capacity, 16));
            m_Length = 0;
            m_Version = 0;
        }

        public ArrayPoolStack(IEnumerable<T> source)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 16;
            }

            m_Array = ArrayPool<T>.Shared.Rent(Math.Max(count, 16));
            m_Length = 0;
            m_Version = 0;


            foreach (var element in source)
            {
                Push(element);
            }
        }


        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Array = null;
            }
            m_Length = 0;
            m_Version = int.MinValue;
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

            return EquatableSpanHelper.IndexOf(m_Array.AsSpan(..m_Length), item) >= 0;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (arrayIndex < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(arrayIndex), 0, array.Length - m_Length, arrayIndex);
            }
            if (arrayIndex > array.Length - m_Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(arrayIndex), 0, array.Length - m_Length, arrayIndex);
            }

            m_Array.AsSpan(..m_Length).CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(Span<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length > span.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(span), 0, span.Length - m_Array.Length, span.Length);
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

            if (capacity < m_Array.Length)
            {
                return m_Array.Length;
            }

            Resize(capacity);
            return m_Array.Length;
        }

        private void Resize(int capacity)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            m_Version++;

            if (CollectionHelper.RoundUpToPowerOf2(Math.Max(capacity, 16)) != m_Array.Length)
            {
                var oldArray = m_Array;
                m_Array = ArrayPool<T>.Shared.Rent(capacity);
                oldArray.AsSpan(..m_Length).CopyTo(m_Array);
                ArrayPool<T>.Shared.Return(oldArray);
            }
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayPoolStack<T> m_Parent;
            private readonly int m_Version;
            private int m_Index;

            internal Enumerator(ArrayPoolStack<T> parent)
            {
                m_Parent = parent;
                m_Version = parent.m_Version;
                m_Index = -1;
            }

            public readonly T Current
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

            return m_Array[m_Length - 1];
        }

        public T Pop()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (m_Length == 0)
            {
                ThrowHelper.ThrowCollectionEmpty();
            }

            m_Version++;
            return m_Array[--m_Length];
        }

        public void Push(T item)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length >= m_Array.Length)
            {
                Resize(m_Array.Length << 1);
            }

            m_Version++;
            m_Array[m_Length++] = item;
        }

        public T[] ToArray()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            var result = new T[m_Length];
            m_Array.AsSpan(..m_Length).CopyTo(result);
            return result;
        }

        public override string ToString()
        {
            return $"{m_Length} items";
        }

        public void TrimExcess()
        {
            TrimExcess(m_Length);
        }

        public void TrimExcess(int capacity)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (capacity < m_Length)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(capacity), m_Length, int.MaxValue, capacity);
            }

            Resize(capacity);
        }

        public bool TryPeek(out T result)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                result = default!;
                return false;
            }

            result = m_Array[m_Length - 1];
            return true;
        }

        public bool TryPop(out T result)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length == 0)
            {
                result = default!;
                return false;
            }

            m_Version++;
            result = m_Array[--m_Length];
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
                ThrowHelper.ThrowArgumentOutOfRange(nameof(index), 0, m_Length, index);
            }
            if (m_Length > array.Length - index)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(index), 0, m_Length - index, index);
            }

            Array.Copy(m_Array, 0, array, index, m_Length);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
