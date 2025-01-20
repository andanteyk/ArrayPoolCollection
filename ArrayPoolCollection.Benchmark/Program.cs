// See https://aka.ms/new-console-template for more information
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using ArrayPoolCollection.Benchmark;
using BenchmarkDotNet.Running;

BenchmarkRunner.Run<BenchmarkTest>();
return;

Console.WriteLine($"{IndexOf([1, 2, 3, 4], 1)}");

Console.WriteLine($"{IndexOf(Enumerable.Repeat(new Hoge(), 4).ToArray(), new Hoge())}");


static int IndexOf<T>(ReadOnlySpan<T> span, T value) => SomeBase<T>.Instance.IndexOf(span, value);

internal class SomeNonGeneric
{
    public static SomeBase<T> GetInstance<T>()
    {
        if (typeof(IEquatable<T>).IsAssignableFrom(typeof(T)))
        {
            return (SomeBase<T>)Activator.CreateInstance(typeof(Some<>).MakeGenericType(typeof(T)))!;
        }
        else
        {
            return new SomeBase<T>();
        }
    }
}

internal class SomeBase<T>
{
    public virtual int IndexOf(ReadOnlySpan<T> span, T value)
    {
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

internal class Hoge { }


