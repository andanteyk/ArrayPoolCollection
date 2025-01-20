using BenchmarkDotNet.Attributes;

namespace ArrayPoolCollection.Benchmark;

[MemoryDiagnoser, DisassemblyDiagnoser]
public class BenchmarkTest
{
    private int[] intArray = new int[1024];
    private FugaStruct[] fugaArray = new FugaStruct[1024];

    [Benchmark]
    public int IndexOfInt()
    {
        return SomeBase<int>.Instance.IndexOf(intArray, 0);
    }

    [Benchmark]
    public int IndexOfFuga()
    {
        return SomeBase<FugaStruct>.Instance.IndexOf(fugaArray, new FugaStruct());
    }



    internal class SomeBase<T>
    {
        public virtual int IndexOf(ReadOnlySpan<T> span, T value)
        {
            var comparer = EqualityComparer<T>.Default;
            foreach (var element in span)
            {
                if (comparer.Equals(element, value))
                {
                    // TODO
                    return 0;
                }
            }
            return -1;
        }

        public static SomeBase<T> Instance { get; }
            = typeof(IEquatable<T>).IsAssignableFrom(typeof(T)) ?
            (SomeBase<T>)Activator.CreateInstance(typeof(Some<>).MakeGenericType(typeof(T)))! :
            new SomeBase<T>();
    }
    internal sealed class Some<T> : SomeBase<T> where T : IEquatable<T>
    {
        public override int IndexOf(ReadOnlySpan<T> span, T value)
        {
            return span.IndexOf(value);
        }
    }

    internal struct FugaStruct { public int Value; }

}
