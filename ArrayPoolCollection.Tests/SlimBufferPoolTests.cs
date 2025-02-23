namespace ArrayPoolCollection.Pool.Tests;

public class SlimBufferPoolTests
{
    [Fact]
    public void Ctor()
    {
        // should not throw any exception
        using var intPool = new SlimBufferPool<int[], int>(new ArrayPoolPolicy<int>());

        // should not throw any exception
        using var stringPool = new SlimBufferPool<string[], string>(new ArrayPoolPolicy<string>());
    }

    [Fact]
    public void Rent_Value()
    {
        var intPool = new SlimBufferPool<int[], int>(new ArrayPoolPolicy<int>());

        Assert.Throws<ArgumentOutOfRangeException>(() => intPool.Rent(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => intPool.Rent(int.MaxValue));
        Assert.Equal(Array.Empty<int>(), intPool.Rent(0));


        var list = new List<int[]>();
        for (int i = 1; i <= 1024; i++)
        {
            var rented = intPool.Rent(i);

            Assert.Equal(CollectionHelper.RoundUpToPowerOf2(Math.Max(i, 16)), rented.Length);

            rented[0] = i;

            list.Add(rented);
        }

        for (int i = 0; i < list.Count; i++)
        {
            Assert.Equal(i + 1, list[i][0]);

            intPool.Return(list[i]);
        }


        intPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => intPool.Rent(1));
    }

    [Fact]
    public void Rent_Class()
    {
        var stringPool = new SlimBufferPool<string[], string>(new ArrayPoolPolicy<string>());

        Assert.Throws<ArgumentOutOfRangeException>(() => stringPool.Rent(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => stringPool.Rent(int.MaxValue));
        Assert.Equal(Array.Empty<string>(), stringPool.Rent(0));


        var list = new List<string[]>();
        for (int i = 1; i <= 1024; i++)
        {
            var rented = stringPool.Rent(i);

            Assert.Equal(CollectionHelper.RoundUpToPowerOf2(Math.Max(i, 16)), rented.Length);

            rented[0] = i.ToString();

            list.Add(rented);
        }

        for (int i = 0; i < list.Count; i++)
        {
            Assert.Equal((i + 1).ToString(), list[i][0]);

            stringPool.Return(list[i]);

            Assert.Null(list[i][0]);
        }


        stringPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stringPool.Rent(1));
    }

    [Fact]
    public void Rent_DisposableHandle_Value()
    {
        var intPool = new SlimBufferPool<int[], int>(new ArrayPoolPolicy<int>());

        Assert.Throws<ArgumentOutOfRangeException>(() => intPool.Rent(-1, out _));

        using (_ = intPool.Rent(0, out var empty))
        {
            Assert.Equal(Array.Empty<int>(), empty);
        }


        var handles = new List<DisposableHandle<int[]>>();
        for (int i = 1; i <= 1024; i++)
        {
            var handle = intPool.Rent(i, out var rented);

            rented[0] = i;

            handles.Add(handle);
        }

        for (int i = 0; i < handles.Count; i++)
        {
            handles[i].Dispose();
        }

        intPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => intPool.Rent(1, out _));
    }

    [Fact]
    public void Rent_DisposableHandle_Class()
    {
        var stringPool = new SlimBufferPool<string[], string>(new ArrayPoolPolicy<string>());

        Assert.Throws<ArgumentOutOfRangeException>(() => stringPool.Rent(-1, out _));

        using (_ = stringPool.Rent(0, out var empty))
        {
            Assert.Equal(Array.Empty<string>(), empty);
        }


        var handles = new List<DisposableHandle<string[]>>();
        for (int i = 1; i <= 1024; i++)
        {
            var handle = stringPool.Rent(i, out var rented);

            rented[0] = i.ToString();

            handles.Add(handle);
        }

        for (int i = 0; i < handles.Count; i++)
        {
            handles[i].Dispose();
        }

        stringPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stringPool.Rent(1, out _));
    }

    [Fact]
    public void Return_Value()
    {
        var intPool = new SlimBufferPool<int[], int>(new ArrayPoolPolicy<int>());

        intPool.Return(Array.Empty<int>());


        for (int i = 1; i <= 1024; i++)
        {
            var rented = intPool.Rent(i);

            rented[0] = i;

            intPool.Return(rented);
        }

        Assert.Throws<InvalidOperationException>(() => intPool.Return(new int[9]));

        {
            var rented = intPool.Rent(16);

            intPool.Return(rented);
        }

        intPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => intPool.Return(Array.Empty<int>()));
    }

    [Fact]
    public void Return_Class()
    {
        var stringPool = new SlimBufferPool<string[], string>(new ArrayPoolPolicy<string>());

        stringPool.Return(Array.Empty<string>());


        for (int i = 1; i <= 1024; i++)
        {
            var rented = stringPool.Rent(i);

            rented[0] = i.ToString();

            stringPool.Return(rented);
            Assert.Null(rented[0]);
        }

        Assert.Throws<InvalidOperationException>(() => stringPool.Return(new string[9]));

        {
            var rented = stringPool.Rent(16);

            stringPool.Return(rented);
        }

        stringPool.Dispose();
        Assert.Throws<ObjectDisposedException>(() => stringPool.Return(Array.Empty<string>()));
    }

    [Fact]
    public void Trim_Value()
    {
        var intPool = new SlimBufferPool<int[], int>(new ArrayPoolPolicy<int>());


        Assert.True(intPool.Trim());


        RentAndThrowAway(intPool);
        GC.Collect();
        Thread.Sleep(10);
        intPool.Trim();


        intPool.Dispose();
        Assert.False(intPool.Trim());
    }

    [Fact]
    public void Trim_Class()
    {
        var stringPool = new SlimBufferPool<string[], string>(new ArrayPoolPolicy<string>());


        Assert.True(stringPool.Trim());


        RentAndThrowAway(stringPool);
        GC.Collect();
        Thread.Sleep(10);
        stringPool.Trim();


        stringPool.Dispose();
        Assert.False(stringPool.Trim());
    }

    private static void RentAndThrowAway<T>(SlimBufferPool<T[], T> pool)
    {
        _ = pool.Rent(16);
    }

    /*
    [Fact]
    public void Huge()
    {
        using var pool = new SlimBufferPool<byte[], byte>(new ArrayPoolPolicy<byte>());

        var rented = pool.Rent((1 << 30) + 1);
        Assert.Equal(CollectionHelper.ArrayMaxLength, rented.Length);
        pool.Return(rented);
    }
    //*/
}
