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
    }
}