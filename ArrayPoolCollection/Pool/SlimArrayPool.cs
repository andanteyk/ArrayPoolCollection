namespace ArrayPoolCollection.Pool
{
    public sealed class SlimArrayPool<T>
    {
        [ThreadStatic]
        private static IBufferPool<T[]>? m_Instance;
        public static IBufferPool<T[]> Shared => m_Instance ??= new SlimBufferPool<T[], T>(new ArrayPoolPolicy<T>());

        private SlimArrayPool() { }
    }
}
