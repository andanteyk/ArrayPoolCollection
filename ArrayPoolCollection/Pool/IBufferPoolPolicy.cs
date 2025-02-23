namespace ArrayPoolCollection.Pool
{
    public interface IBufferPoolPolicy<TBuffer, TElement>
        where TBuffer : class
    {
        public TBuffer Create(int length);
        public Span<TElement> AsSpan(TBuffer value);
    }

    internal sealed class ArrayPoolPolicy<T> : IBufferPoolPolicy<T[], T>
    {
        public Span<T> AsSpan(T[] value)
        {
            return value.AsSpan();
        }

        public T[] Create(int length)
        {
            return CollectionHelper.AllocateUninitializedArray<T>(length);
        }
    }
}