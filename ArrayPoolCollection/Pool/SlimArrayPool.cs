namespace ArrayPoolCollection.Pool
{
    public sealed class SlimArrayPool<T>
    {
        [ThreadStatic]
        private static SlimBufferPool<T[], T>? m_Instance;
        public static SlimBufferPool<T[], T> Shared => m_Instance ??= new SlimBufferPool<T[], T>(new ArrayPoolPolicy<T>());

        private SlimArrayPool() { }
    }
}
