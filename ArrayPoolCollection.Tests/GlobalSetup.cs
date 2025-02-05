using System.Diagnostics.CodeAnalysis;

[assembly: AssemblyFixture(typeof(ArrayPoolCollection.MemoryPack.Tests.GlobalSetup))]
[assembly: SuppressMessage("Usage", "xUnit2017", Justification = "To test Contains() itself")]

namespace ArrayPoolCollection.MemoryPack.Tests;

public class GlobalSetup : IDisposable
{
    public GlobalSetup()
    {
        ArrayPoolCollectionRegisterer.Register();
    }

    public void Dispose()
    {
    }
}
