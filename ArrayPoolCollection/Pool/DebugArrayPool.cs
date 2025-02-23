namespace ArrayPoolCollection.Pool
{
    public sealed class DebugArrayPool<T>
    {
        [ThreadStatic]
        private static IBufferPool<T[]>? m_Instance;
        public static IBufferPool<T[]> Shared => m_Instance ??= new DebugBufferPool<T[], T>(new ArrayPoolPolicy<T>());

        private DebugArrayPool() { }
    }
}
