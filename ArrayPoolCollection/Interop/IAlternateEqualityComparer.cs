#if !NET9_0_OR_GREATER
namespace System.Collections.Generic
{
    public interface IAlternateEqualityComparer<in TAlternate, T>
    {
        public bool Equals(TAlternate alternate, T other);
        public int GetHashCode(TAlternate alternate);
        public T Create(TAlternate alternate);
    }
}
#endif
