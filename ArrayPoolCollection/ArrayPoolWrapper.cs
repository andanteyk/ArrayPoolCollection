using System.Buffers;
using System.Collections;
using System.Runtime.CompilerServices;

namespace ArrayPoolCollection
{
    public class ArrayPoolWrapper<T> : IList<T>, IReadOnlyList<T>, IDisposable
    {
        private T[]? Array;
        private int Length;

        public ArrayPoolWrapper(int length) : this(length, true) { }

        public ArrayPoolWrapper(int length, bool clearArray)
        {
            Array = ArrayPool<T>.Shared.Rent(length);
            Length = length;

            if (clearArray)
            {
                Array.AsSpan(..Length).Clear();
            }
        }

        public Span<T> AsSpan()
        {
            if (Array == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
            }

            return Array.AsSpan(..Length);
        }

        public static implicit operator Span<T>(ArrayPoolWrapper<T> source)
        {
            return source.AsSpan();
        }


        public T this[int index]
        {
            get
            {
                if ((uint)index >= Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(Length, index);
                }
                if (Array == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                return Array[index];
            }
            set
            {
                if ((uint)index >= Length)
                {
                    ThrowHelper.ThrowIndexOutOfRange(Length, index);
                }
                if (Array == null)
                {
                    ThrowHelper.ThrowObjectDisposed(nameof(Array));
                }
                Array[index] = value;
            }
        }

        public int Count => Length;

        public bool IsReadOnly => false;

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
            if (Array != null)
            {
                ArrayPool<T>.Shared.Return(Array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                Array = null;
            }
        }

        public Enumerator GetEnumerator()
        {
            if (Array == null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(Array));
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
                    if ((uint)Index >= Source.Length)
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
                if (Index >= Source.Length)
                {
                    return false;
                }

                return ++Index < Source.Length;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }
        }
    }
}
