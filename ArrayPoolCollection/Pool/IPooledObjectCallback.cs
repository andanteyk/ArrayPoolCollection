namespace ArrayPoolCollection.Pool
{
    public interface IPooledObjectCallback<T>
    {
        public T OnInstantiate();
        public void OnRent(T item);
        public void OnReturn(T item);
        public void OnDestroy(T item);
    }
}
