using System.Runtime.ConstrainedExecution;

namespace ArrayPoolCollection.Pool
{
    internal class GabageCollectorCallback : CriticalFinalizerObject
    {
        private readonly Action m_Callback;

        internal static void Register(Action callback)
        {
            new GabageCollectorCallback(callback);
        }

        private GabageCollectorCallback(Action callback)
        {
            m_Callback = callback;
        }

        ~GabageCollectorCallback()
        {
            m_Callback();
            GC.ReRegisterForFinalize(this);
        }
    }
}
