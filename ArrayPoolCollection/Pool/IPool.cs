namespace ArrayPoolCollection.Pool
{
    public interface IPool<T>
    {
        public T Rent();
        public DisposableHandle<T> Rent(out T result)
        {
            result = Rent();
            return new DisposableHandle<T>(result, this);
        }

        public void Return(T value);

        public bool TrimExcess();
    }
}
