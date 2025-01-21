using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;

#if NET5_0_OR_GREATER
using System.Runtime.InteropServices;
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

                // TODO: my own classes

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
    }
}