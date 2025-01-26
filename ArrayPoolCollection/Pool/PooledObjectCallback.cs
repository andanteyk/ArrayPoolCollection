namespace ArrayPoolCollection.Pool
{
    public static class PooledObjectCallback
    {
        public static PooledObjectCallback<T> Create<T>()
            where T : new()
        {
            return new PooledObjectCallback<T>(
                () => new T(),
                x => { },
                x => { },
                x =>
                {
                    if (x is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                });
        }
    }

    public class PooledObjectCallback<T> : IPooledObjectCallback<T>
    {
        private readonly Func<T> m_OnInstantiate;
        private readonly Action<T> m_OnRent;
        private readonly Action<T> m_OnReturn;
        private readonly Action<T> m_OnDestroy;


        public static PooledObjectCallback<T> Create(Func<T> onInstantiate, Action<T> onRent, Action<T> onReturn, Action<T> onDestroy)
        {
            return new PooledObjectCallback<T>(onInstantiate, onRent, onReturn, onDestroy);
        }

        internal PooledObjectCallback(Func<T> onInstantiate, Action<T> onRent, Action<T> onReturn, Action<T> onDestroy)
        {
            m_OnInstantiate = onInstantiate;
            m_OnRent = onRent;
            m_OnReturn = onReturn;
            m_OnDestroy = onDestroy;
        }

        public T OnInstantiate()
        {
            return m_OnInstantiate();
        }

        public void OnDestroy(T item)
        {
            m_OnDestroy(item);
        }

        public void OnRent(T item)
        {
            m_OnRent(item);
        }

        public void OnReturn(T item)
        {
            m_OnReturn(item);
        }
    }
}
