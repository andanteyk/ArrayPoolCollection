namespace ArrayPoolCollection.Pool.Tests;

[CollectionDefinition(DisableParallelization = true)]
public class ObjectPoolTests()
{
    [Fact]
    public void Shared()
    {
        var random = ObjectPool<Random>.Shared.Rent();
        random.Next();
        ObjectPool<Random>.Shared.Return(random);
    }

    [Fact]
    public void Ctor()
    {
        using var pool = new ObjectPool<Random>(PooledObjectCallback.Create<Random>(), 256);
    }

    [Fact]
    public void Rent()
    {
        int instantiateCount = 0;
        int rentCount = 0;
        var pool = new ObjectPool<Random>(new PooledObjectCallback<Random>(
            () => { instantiateCount++; return new Random(); },
            i => rentCount++,
            i => { },
            i => { }
        ), 256);

        for (int i = 0; i < 1024; i++)
        {
            var random = pool.Rent();
            pool.Return(random);
        }

        Assert.Equal(1, instantiateCount);
        Assert.Equal(1024, rentCount);


        pool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pool.Rent());
    }

    [Fact]
    public void Return()
    {
        int returnCount = 0;
        var pool = new ObjectPool<Random>(new PooledObjectCallback<Random>(
            () => new Random(),
            i => { },
            i => returnCount++,
            i => { }
         ), 256);

        var stack = new Stack<Random>();
        for (int i = 0; i < 1024; i++)
        {
            stack.Push(pool.Rent());
        }

        for (int i = 0; i < 1024; i++)
        {
            pool.Return(stack.Pop());
        }
        Assert.Equal(1024, returnCount);


        Assert.Throws<ArgumentNullException>(() => pool.Return(default!));


        pool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pool.Return(default!));
    }

    [Fact]
    public void Prewarm()
    {
        int instantiateCount = 0;
        int returnCount = 0;
        var pool = new ObjectPool<Random>(new PooledObjectCallback<Random>(
            () => { instantiateCount++; return new Random(); },
            i => { },
            i => returnCount++,
            i => { }
         ), 256);

        pool.Prewarm(1024);
        Assert.Equal(1024, instantiateCount);
        Assert.Equal(1024, returnCount);

        instantiateCount = 0;
        for (int i = 0; i < 1024; i++)
        {
            _ = pool.Rent();
        }
        Assert.Equal(0, instantiateCount);


        Assert.Throws<ArgumentOutOfRangeException>(() => pool.Prewarm(-1));


        pool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pool.Prewarm(10));
    }

    [Fact]
    public void TrimExcess()
    {
        int instantiateCount = 0;
        int returnCount = 0;
        int destroyCount = 0;
        var pool = new ObjectPool<Random>(new PooledObjectCallback<Random>(
            () => { instantiateCount++; return new Random(); },
            i => { },
            i => returnCount++,
            i => destroyCount++
         ), 256);

        pool.Prewarm(1024);

        pool.TrimExcess();
        Assert.Equal(1024 - 256, destroyCount);


        pool.Dispose();
        Assert.Equal(1024, destroyCount);
        // should not throw any exceptions
        pool.TrimExcess();
    }

    private record class DisposeDetector(Action Callback) : IDisposable
    {
        public void Dispose()
        {
            Callback();
        }
    }

    [Fact]
    public void Dispose()
    {
        int disposed = 0;

        var pool = new ObjectPool<DisposeDetector>(PooledObjectCallback<DisposeDetector>.Create(
            () => new DisposeDetector(() => disposed++),
            i => { },
            i => { },
            i => i.Dispose()
        ));

        pool.Prewarm(16);
        pool.Dispose();

        Assert.Equal(16, disposed);

        // should not throw any exceptions
        pool.Dispose();

        Assert.Throws<InvalidOperationException>(() => ObjectPool<DisposeDetector>.Shared.Dispose());
    }

    [Fact]
    public void GcTest()
    {
        int instantiateCount = 0;
        int returnCount = 0;
        int destroyCount = 0;
        using var pool = new ObjectPool<Random>(new PooledObjectCallback<Random>(
            () => { instantiateCount++; return new Random(); },
            i => { },
            i => returnCount++,
            i => destroyCount++
         ), 256, true);

        pool.Prewarm(1024);

        Thread.Sleep(10);
        GC.Collect(2);
        Thread.Sleep(10);

        Assert.Equal(1024 - 256, destroyCount);
    }
}
