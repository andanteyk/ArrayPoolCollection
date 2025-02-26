using System.Collections;
using System.Runtime.CompilerServices;
using ArrayPoolCollection.Pool;

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

            m_Array = SlimArrayPool<T>.Shared.Rent(Math.Max(capacity, 16));
            m_Length = 0;
            m_Version = 0;
        }

        public ArrayPoolStack(IEnumerable<T> source)
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 16;
            }

            m_Array = SlimArrayPool<T>.Shared.Rent(Math.Max(count, 16));
            m_Length = 0;
            m_Version = 0;

            PushRange(source);
        }

        /// <summary>
        /// `AsSpan()` works similarly to `CollectionsMarshal.AsSpan()`.
        /// Note that adding or removing elements from a collection may reference discarded buffers.
        /// </summary>
        public static Span<T> AsSpan(ArrayPoolStack<T> stack)
        {
            if (stack.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return stack.m_Array.AsSpan(..stack.m_Length);
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                SlimArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
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

            Resize(Math.Max(capacity, 16));
            return m_Array.Length;
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
            m_Array = SlimArrayPool<T>.Shared.Rent(newCapacity);

            if (oldArray is not null)
            {
                oldArray.AsSpan(..m_Length).CopyTo(m_Array);
                SlimArrayPool<T>.Shared.Return(oldArray, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
            }

            m_Version++;
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
                m_Index = parent.m_Length;
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

            public readonly void Dispose()
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

                if (m_Index < 0)
                {
                    return false;
                }

                return --m_Index >= 0;
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

                m_Index = m_Parent.m_Length;
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

        public void PushRange(IEnumerable<T> items)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (CollectionHelper.TryGetNonEnumeratedCount(items, out int count))
            {
                if (m_Length + count > m_Array.Length)
                {
                    Resize(m_Length + count);
                }
            }

            if (items is ICollection<T> collection)
            {
                collection.CopyTo(m_Array, m_Length);
                m_Length += count;
                m_Version++;
            }
            else
            {
                foreach (var item in items)
                {
                    Push(item);
                }
            }
        }

        public void PushRange(T[] items) => PushRange(items.AsSpan());

        public void PushRange(ReadOnlySpan<T> span)
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            if (m_Length + span.Length > m_Array.Length)
            {
                Resize(m_Length + span.Length);
            }

            span.CopyTo(m_Array.AsSpan(m_Length..));
            m_Length += span.Length;
            m_Version++;
        }

        /// <summary>
        /// `SetCount()` works similarly as `CollectionsMarshal.SetCount()`.
        /// Use with caution as it may reference uninitialized area.
        /// </summary>
        public static void SetCount(ArrayPoolStack<T> stack, int count)
        {
            if (stack.m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, stack.m_Array.Length, count);
            }
            if (count > stack.m_Array.Length)
            {
                ThrowHelper.ThrowArgumentOverLength(nameof(count), 0, stack.m_Array.Length, count);
            }

            stack.m_Length = count;
            stack.m_Version++;
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

            m_Version++;
            Resize(Math.Max(capacity, 16));
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
