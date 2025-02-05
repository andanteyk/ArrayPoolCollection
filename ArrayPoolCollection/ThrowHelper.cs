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

        [DoesNotReturn]
        internal static void ThrowKeyNotFound(string? key)
        {
            throw new KeyNotFoundException($"key {key} not found");
        }

        [DoesNotReturn]
        internal static void ThrowKeyIsAlreadyExists(string? key)
        {
            throw new ArgumentException($"key {key} is already exists");
        }

        [DoesNotReturn]
        internal static void ThrowNotAlternateComparer()
        {
            throw new InvalidOperationException("Comparer is not IAlternateEqualityComparer<TAlternate, TKey>");
        }

        [DoesNotReturn]
        internal static void ThrowCollectionEmpty()
        {
            throw new InvalidOperationException("Collection is empty");
        }

        [DoesNotReturn]
        internal static void ThrowHasNoConstructor()
        {
            throw new InvalidOperationException("This type has no `new()`");
        }

        [DoesNotReturn]
        internal static void ThrowDontDisposeShared()
        {
            throw new InvalidOperationException("Must not dispose Shared instance");
        }

        [DoesNotReturn]
        internal static void ThrowLengthIsDifferent()
        {
            throw new ArgumentException("Each element has a different length");
        }

        [DoesNotReturn]
        internal static void ThrowOutOfMemory()
        {
            throw new OutOfMemoryException("Array.Length exceeded supported range.");
        }

        [DoesNotReturn]
        internal static void ThrowDoesNotImplement<T>(string name)
        {
            throw new InvalidOperationException($"{name} does not implement {typeof(T).Name}");
        }
    }
}
