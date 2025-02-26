namespace ArrayPoolCollection.Pool
{
    public readonly struct DisposableHandle<T> : IDisposable
    {
        private readonly T Value;
        private readonly IPool<T> Pool;

        internal DisposableHandle(T value, IPool<T> pool)
        {
            Value = value;
            Pool = pool;
        }

        public void Dispose()
        {
            Pool.Return(Value);
        }
    }
}