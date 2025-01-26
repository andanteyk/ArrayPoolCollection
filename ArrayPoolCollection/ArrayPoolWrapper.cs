using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public sealed class ArrayPoolWrapper<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable
    {
        private T[]? m_Array;
        private readonly int m_Length;

        public ArrayPoolWrapper(int length) : this(length, true) { }

        public ArrayPoolWrapper(int length, bool clearArray)
        {
            m_Array = ArrayPool<T>.Shared.Rent(length);
            m_Length = length;

            if (clearArray)
            {
                m_Array.AsSpan(..m_Length).Clear();
            }
        }

        public Span<T> AsSpan()
        {
            if (m_Array is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
            }

            return m_Array.AsSpan(..m_Length);
        }

        public static implicit operator Span<T>(ArrayPoolWrapper<T> source)
        {
            return source.AsSpan();
        }


        public T this[int index]
        {
            get
            {
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                return m_Array[index];
            }
            set
            {
                if ((uint)index >= m_Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(m_Length, index);
                }
                if (m_Array is null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(m_Array));
                }
                m_Array[index] = value;
            }
        }

        public int Count => m_Length;

        public bool IsReadOnly => false;

        bool IList.IsFixedSize => throw new NotImplementedException();

        bool IList.IsReadOnly => throw new NotImplementedException();

        int ICollection.Count => throw new NotImplementedException();

        bool ICollection.IsSynchronized => throw new NotImplementedException();

        object ICollection.SyncRoot => throw new NotImplementedException();

        object? IList.this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void Add(T item)
        {
            throw new NotSupportedException();
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        public bool Contains(T item)
        {
            return EquatableSpanHelper.IndexOf(AsSpan(), item) != -1;
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            AsSpan().CopyTo(array.AsSpan(arrayIndex..));
        }

        public void CopyTo(Span<T> span)
        {
            AsSpan().CopyTo(span);
        }

        public void Dispose()
        {
            if (m_Array is not null)
            {
                ArrayPool<T>.Shared.Return(m_Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                m_Array = null;
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

        public int IndexOf(T item)
        {
            return EquatableSpanHelper.IndexOf(AsSpan(), item);
        }

        public void Insert(int index, T item)
        {
            throw new NotSupportedException();
        }

        public bool Remove(T item)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(int index)
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

            System.Array.Copy(m_Array, 0, array, index, m_Length);
        }

        public struct Enumerator : IEnumerator<T>
        {
            private readonly ArrayPoolWrapper<T> Source;
            private int Index;

            internal Enumerator(ArrayPoolWrapper<T> source)
            {
                Source = source;
                Index = -1;
            }

            public readonly T Current
            {
                get
                {
                    if ((uint)Index >= Source.m_Length)
                    {
                        throw new InvalidOperationException();
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
                if (Index >= Source.m_Length)
                {
                    return false;
                }

                return ++Index < Source.m_Length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
