namespace ArrayPoolCollection.Pool
{
    public sealed class ObjectPool<T> : IDisposable
    {
        [ThreadStatic]
        private static ObjectPool<T>? m_Shared;
        public static ObjectPool<T> Shared
        {
            get
            {
                if (m_Shared is not null)
                {
                    return m_Shared;
                }

                if (typeof(T).GetConstructor(Type.EmptyTypes) is not null)
                {
                    m_Shared = new ObjectPool<T>(PooledObjectCallback<T>.Create(
                        () => Activator.CreateInstance<T>(),
                        x => { },
                        x => { },
                        x =>
                        {
                            if (x is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    ), DefaultTrimThreshold);
                    GabageCollectorCallback.Register(() => m_Shared?.TrimExcess());
                    return m_Shared;
                }
                else
                {
                    ThrowHelper.ThrowHasNoConstructor();
                    return default;
                }
            }
        }


        private const int DefaultTrimThreshold = 256;
        public int TrimThreshold { get; init; } = DefaultTrimThreshold;

        private ArrayPoolQueue<T>? m_Queue;
        private readonly IPooledObjectCallback<T> m_Callback;

        public ObjectPool(IPooledObjectCallback<T> callback) : this(callback, DefaultTrimThreshold) { }
        public ObjectPool(IPooledObjectCallback<T> callback, int trimThreshold)
        {
            m_Queue = new();
            m_Callback = callback;
            TrimThreshold = trimThreshold;
        }

        public T Rent()
        {
            if (m_Queue is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Queue));
            }

            if (m_Queue.TryDequeue(out var value))
            {
                m_Callback.OnRent(value);
                return value;
            }
            else
            {
                var newValue = m_Callback.OnInstantiate();
                m_Callback.OnRent(newValue);
                return newValue;
            }
        }

        public void Return(T value)
        {
            if (m_Queue is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Queue));
            }
            if (!typeof(T).IsValueType && value is null)
            {
                ThrowHelper.ThrowArgumentIsNull(nameof(value));
            }

            m_Callback.OnReturn(value);
            m_Queue.Enqueue(value);
        }

        public void Prewarm(int count)
        {
            if (m_Queue is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Queue));
            }
            if (count < 0)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(count), 0, int.MaxValue, count);
            }

            while (m_Queue.Count < count)
            {
                var value = m_Callback.OnInstantiate();
                m_Callback.OnReturn(value);
                m_Queue.Enqueue(value);
            }
        }

        public void TrimExcess()
        {
            if (m_Queue is null)
            {
                // this may call from GabageCollectorCallback; should not throw
                return;
            }

            while (m_Queue.Count > TrimThreshold)
            {
                var value = m_Queue.Dequeue();
                m_Callback.OnDestroy(value);
            }
            m_Queue.TrimExcess();
        }

        public void Dispose()
        {
            if (m_Shared == this)
            {
                ThrowHelper.ThrowDontDisposeShared();
            }
            if (m_Queue is not null)
            {
                foreach (var item in m_Queue)
                {
                    m_Callback.OnDestroy(item);
                }

                m_Queue.Dispose();
                m_Queue = null;
            }
        }
    }
}
