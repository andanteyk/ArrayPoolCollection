namespace ArrayPoolCollection.Pool
{
    public interface IBufferPool<T> : IPool<T>
    {
        public T Rent(int minimumLength);
        public DisposableHandle<T> Rent(int minimumLength, out T result)
        {
            result = Rent(minimumLength);
            return new DisposableHandle<T>(result, this);
        }

        T IPool<T>.Rent() => Rent(0);
        DisposableHandle<T> IPool<T>.Rent(out T result) => Rent(0, out result);
    }
}
