using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection
{
    internal ref struct SegmentedArray<T>
    {
        private readonly Span<T> StackSegment;
        private Array27 Arrays;
        private Span<T> CurrentSegment;
        private int CurrentSegmentIndex;
        private int SegmentIndex;

        public SegmentedArray(Span<T> stackSegment)
        {
            StackSegment = CurrentSegment = stackSegment;
            CurrentSegmentIndex = 0;
            SegmentIndex = -1;
        }

        public void Dispose()
        {
            foreach (var array in Arrays.AsSpan())
            {
                if (array is not null)
                {
                    ArrayPool<T>.Shared.Return(array, RuntimeHelpers.IsReferenceOrContainsReferences<T>());
                }
                else
                {
                    break;
                }
            }
        }

        public void Add(T item)
        {
            if (CurrentSegmentIndex < CurrentSegment.Length)
            {
                CurrentSegment[CurrentSegmentIndex++] = item;
                return;
            }

            SegmentIndex++;
            CurrentSegment = Arrays.AsSpan()[SegmentIndex] = ArrayPool<T>.Shared.Rent(1 << (SegmentIndex + 4));
            CurrentSegment[0] = item;
            CurrentSegmentIndex = 1;
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (CollectionHelper.TryGetSpan(items, out var span))
            {
                AddRange(span);
                return;
            }

            foreach (var item in items)
            {
                Add(item);
            }
        }

        public void AddRange(ReadOnlySpan<T> items)
        {
            while (true)
            {
                var destinationRange = CurrentSegment[CurrentSegmentIndex..];
                var sourceRange = items[..Math.Min(items.Length, destinationRange.Length)];
                sourceRange.CopyTo(destinationRange);
                items = items[sourceRange.Length..];

                if (items.Length == 0)
                {
                    CurrentSegmentIndex = sourceRange.Length;
                    break;
                }

                SegmentIndex++;
                CurrentSegment = Arrays.AsSpan()[SegmentIndex] = ArrayPool<T>.Shared.Rent(1 << (SegmentIndex + 4));
                CurrentSegmentIndex = 0;
            }
        }

        public void CopyTo(Span<T> destination)
        {
            if (destination.Length < GetTotalLength())
            {
                ThrowHelper.ThrowDestinationTooShort();
            }

            if (SegmentIndex == -1)
            {
                StackSegment[..CurrentSegmentIndex].CopyTo(destination);
                return;
            }
            else
            {
                StackSegment.CopyTo(destination);
                destination = destination[StackSegment.Length..];
            }

            int i = 0;
            foreach (var array in Arrays.AsSpan())
            {
                if (SegmentIndex == i)
                {
                    array.AsSpan(..CurrentSegmentIndex).CopyTo(destination);
                    return;
                }
                else
                {
                    array.CopyTo(destination);
                    destination = destination[array.Length..];
                }

                i++;
            }
        }

        public T[] ToArrayPool(out Span<T> span)
        {
            int totalLength = GetTotalLength();
            var result = ArrayPool<T>.Shared.Rent(totalLength);
            CopyTo(result);
            span = result.AsSpan(..totalLength);
            return result;
        }

        public T[] ToArray()
        {
            int totalLength = GetTotalLength();

            var result = CollectionHelper.AllocateUninitializedArray<T>(totalLength);
            CopyTo(result);
            return result;
        }

        public List<T> ToList()
        {
            int totalLength = GetTotalLength();
            var list = new List<T>(totalLength);

            CollectionHelper.SetListCount(list, totalLength);
            CopyTo(CollectionHelper.ListToSpan(list));

            return list;
        }

        public int GetTotalLength()
        {
            if (SegmentIndex == -1)
            {
                return CurrentSegmentIndex;
            }
            return StackSegment.Length + Arrays.AsSpan()[SegmentIndex].Length - 16 + CurrentSegmentIndex;
        }

        // handcrafted [InlineArray(16)]
        internal struct Stack16
        {
#pragma warning disable CS0649
            public T Value0;
            public T Value1;
            public T Value2;
            public T Value3;
            public T Value4;
            public T Value5;
            public T Value6;
            public T Value7;
            public T Value8;
            public T Value9;
            public T Value10;
            public T Value11;
            public T Value12;
            public T Value13;
            public T Value14;
            public T Value15;
#pragma warning restore

            public static Stack16 Create()
            {
                Unsafe.SkipInit<Stack16>(out var result);
                return result;
            }
            public Span<T> AsSpan() => MemoryMarshal.CreateSpan(ref Value0, 16);
        }

        // handcrafted [InlineArray(27)]
        private struct Array27
        {
#pragma warning disable CS0649
            public T[] Array0;
            public T[] Array1;
            public T[] Array2;
            public T[] Array3;
            public T[] Array4;
            public T[] Array5;
            public T[] Array6;
            public T[] Array7;
            public T[] Array8;
            public T[] Array9;
            public T[] Array10;
            public T[] Array11;
            public T[] Array12;
            public T[] Array13;
            public T[] Array14;
            public T[] Array15;
            public T[] Array16;
            public T[] Array17;
            public T[] Array18;
            public T[] Array19;
            public T[] Array20;
            public T[] Array21;
            public T[] Array22;
            public T[] Array23;
            public T[] Array24;
            public T[] Array25;
            public T[] Array26;
#pragma warning restore

            public Span<T[]> AsSpan() => MemoryMarshal.CreateSpan(ref Array0, 27);
        }
    }
}