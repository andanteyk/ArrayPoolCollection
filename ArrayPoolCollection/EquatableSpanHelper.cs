namespace ArrayPoolCollection
{
    internal class EquatableSpanHelper
    {
        internal static int IndexOf<T>(ReadOnlySpan<T> span, T value)
        {
            return FallbackImpl<T>.Instance.IndexOf(span, value);
        }

        private class FallbackImpl<T>
        {
            public virtual int IndexOf(ReadOnlySpan<T> span, T value)
            {
                var comparer = EqualityComparer<T>.Default;

                int i = 0;
                foreach (var element in span)
                {
                    if (comparer.Equals(element, value))
                    {
                        return i;
                    }
                    i++;
                }
                return -1;
            }

            public static FallbackImpl<T> Instance { get; }
                = typeof(IEquatable<T>).IsAssignableFrom(typeof(T)) ?
                (FallbackImpl<T>)Activator.CreateInstance(typeof(EquatableImpl<>).MakeGenericType(typeof(T)))! :
                new FallbackImpl<T>();
        }

        private sealed class EquatableImpl<T> : FallbackImpl<T>
            where T : IEquatable<T>
        {
            public override int IndexOf(ReadOnlySpan<T> span, T value)
            {
                return span.IndexOf(value);
            }
        }
    }
}
