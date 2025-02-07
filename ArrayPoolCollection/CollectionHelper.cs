using System.Collections;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
#endif

#if NET6_0_OR_GREATER
using System.Numerics;
#endif

namespace ArrayPoolCollection
{
    internal static class CollectionHelper
    {
        internal static Span<T> ListToSpan<T>(List<T> list)
        {
#if NET5_0_OR_GREATER
            return CollectionsMarshal.AsSpan(list);
#else
            return Unsafe.As<List<T>, ListClone<T>>(ref list).Items.AsSpan(..list.Count);
#endif
        }

        internal static void SetListCount<T>(List<T> list, int count)
        {
#if NET8_0_OR_GREATER
            CollectionsMarshal.SetCount(list, count);
#else
            Unsafe.As<List<T>, ListClone<T>>(ref list).Size = count;
#endif
        }

        private class ListClone<T>
        {
            public T[] Items = default!;
            public int Size = 0;
            public int Version = 0;
        }

        internal static bool TryGetSpan<T>(IEnumerable<T> source, out Span<T> span)
        {
            switch (source)
            {
                case T[] array:
                    span = array;
                    return true;

                case List<T> list:
                    span = ListToSpan(list);
                    return true;

                case ArrayPoolWrapper<T> arrayPoolWrapper:
                    span = arrayPoolWrapper;
                    return true;

                case ArrayPoolList<T> arrayPoolList:
                    span = ArrayPoolList<T>.AsSpan(arrayPoolList);
                    return true;

                default:
                    span = default;
                    return false;
            }
        }

        internal static bool TryGetNonEnumeratedCount<T>(IEnumerable<T> source, out int count)
        {
#if NET6_0_OR_GREATER
            return source.TryGetNonEnumeratedCount(out count);
#else
            if (source is ICollection<T> genericCollection)
            {
                count = genericCollection.Count;
                return true;
            }
            if (source is ICollection collection)
            {
                count = collection.Count;
                return true;
            }
            count = default;
            return false;
#endif
        }

        internal static T[] AllocateUninitializedArray<T>(int length)
        {
#if NET5_0_OR_GREATER
            return GC.AllocateUninitializedArray<T>(length);
#else
            return new T[length];
#endif
        }

        internal static int RoundUpToPowerOf2(int value)
        {
#if NET6_0_OR_GREATER
            return (int)BitOperations.RoundUpToPowerOf2((uint)value);
#else
            uint x = (uint)(value - 1);
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;
            return (int)(x + 1);
#endif
        }

        internal static int TrailingZeroCount(ulong x)
        {
#if NETCOREAPP3_0_OR_GREATER
            return BitOperations.TrailingZeroCount(x);
#else
            int c = 63;
            x &= ~x + 1;
            if ((x & 0x00000000ffffffff) != 0) c -= 32;
            if ((x & 0x0000ffff0000ffff) != 0) c -= 16;
            if ((x & 0x00ff00ff00ff00ff) != 0) c -= 8;
            if ((x & 0x0f0f0f0f0f0f0f0f) != 0) c -= 4;
            if ((x & 0x3333333333333333) != 0) c -= 2;
            if ((x & 0x5555555555555555) != 0) c -= 1;
            return c;
#endif
        }

        internal static ref T NullRef<T>()
        {
            return ref Unsafe.NullRef<T>();
        }

        internal static int ArrayMaxLength =>
#if NET6_0_OR_GREATER
            Array.MaxLength;    // 0x7FFFFFC7
#else
            0x7FEFFFFF;
#endif

        internal static int GetInitialPoolingSize(int size)
        {
            if (size < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(size), 0, ArrayMaxLength, size);
            }

            int pow2 = RoundUpToPowerOf2(Math.Max(size, 16));
            if (pow2 == int.MinValue)
            {
                pow2 = ArrayMaxLength;
            }

            return pow2;
        }

        internal static int GetNextPoolingSize(int currentSize)
        {
            if (currentSize < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(currentSize), 0, ArrayMaxLength, currentSize);
            }
            if (currentSize == ArrayMaxLength)
            {
                ThrowHelper.ThrowOutOfMemory();
            }

            int next = currentSize << 1;
            if (next == int.MinValue)
            {
                next = ArrayMaxLength;
            }

            return next;
        }
    }
}