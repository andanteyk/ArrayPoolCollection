using System.Diagnostics.CodeAnalysis;

namespace ArrayPoolCollection
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        internal static void ThrowObjectDisposed(string name)
        {
            throw new ObjectDisposedException(name);
        }

        [DoesNotReturn]
        internal static void ThrowIndexOutOfRange(int max, int requested)
        {
            throw new IndexOutOfRangeException($"Range is 0..{max} but {requested} was requested");
        }

        [DoesNotReturn]
        internal static void ThrowDestinationTooShort()
        {
            throw new ArgumentException($"Destination too short");
        }
    }
}
