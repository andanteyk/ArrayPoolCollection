namespace ArrayPoolCollection.Pool
{
    public sealed class DebugArrayPool<T>
    {
        [ThreadStatic]
        private static DebugBufferPool<T[], T>? m_Instance;
        public static DebugBufferPool<T[], T> Shared => m_Instance ??= new DebugBufferPool<T[], T>(new ArrayPoolPolicy<T>());

        private DebugArrayPool() { }
    }
}
