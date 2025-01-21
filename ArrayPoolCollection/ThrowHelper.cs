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
        internal static void ThrowArgumentOutOfRange(string name, int min, int max, int requested)
        {
            throw new ArgumentOutOfRangeException(name, $"Range is {min}..{max} but {requested} was requested");
        }

        [DoesNotReturn]
        internal static void ThrowArgumentOverLength(string name, int min, int max, int requested)
        {
            throw new ArgumentException(name, $"Range is {min}..{max} but {requested} was requested");
        }

        [DoesNotReturn]
        internal static void ThrowArgumentIsNull(string name)
        {
            throw new ArgumentNullException($"{name} should not be null");
        }

        [DoesNotReturn]
        internal static void ThrowDestinationTooShort()
        {
            throw new ArgumentException("Destination too short");
        }

        [DoesNotReturn]
        internal static void ThrowDifferentVersion()
        {
            throw new InvalidOperationException("The elements of the collection have changed. Collection cannot be modified while enumerating.");
        }

        [DoesNotReturn]
        internal static void ThrowArgumentTypeMismatch(string name)
        {
            throw new ArgumentException($"The type of {name} cannot be assigned to this collection");
        }

        [DoesNotReturn]
        internal static void ThrowEnumeratorUndefined()
        {
            throw new InvalidOperationException("The state of the enumerator is undefined");
        }
    }
}
