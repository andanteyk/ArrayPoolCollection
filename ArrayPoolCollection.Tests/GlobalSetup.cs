[assembly: AssemblyFixture(typeof(ArrayPoolCollection.MemoryPack.Tests.GlobalSetup))]

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
