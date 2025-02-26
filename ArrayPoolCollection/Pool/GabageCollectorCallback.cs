using System.Runtime.ConstrainedExecution;

namespace ArrayPoolCollection.Pool
{
    internal class GabageCollectorCallback : CriticalFinalizerObject
    {
        private readonly Func<bool> m_Callback;

        internal static void Register(Func<bool> callback)
        {
            _ = new GabageCollectorCallback(callback);
        }

        private GabageCollectorCallback(Func<bool> callback)
        {
            m_Callback = callback;
        }

        ~GabageCollectorCallback()
        {
            if (m_Callback())
            {
                GC.ReRegisterForFinalize(this);
            }
        }
    }
}
