namespace ArrayPoolCollection.Buffers.Tests;

public class ArrayPoolBufferWriterTests
{
    [Fact]
    public void Capacity()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Equal(64, writer.Capacity);
        writer.Advance(16);
        Assert.Equal(64, writer.Capacity);


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.Capacity);
    }

    [Fact]
    public void FreeCapacity()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Equal(64, writer.FreeCapacity);
        writer.Advance(16);
        Assert.Equal(48, writer.FreeCapacity);


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.FreeCapacity);
    }

    [Fact]
    public void WrittenCount()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Equal(0, writer.WrittenCount);
        writer.Advance(16);
        Assert.Equal(16, writer.WrittenCount);


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.WrittenCount);
    }

    [Fact]
    public void WrittenMemory()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Equal(0, writer.WrittenMemory.Length);

        var memory = writer.GetMemory(16);
        for (int i = 0; i < memory.Length; i++)
        {
            memory.Span[i] = (byte)i;
        }
        writer.Advance(16);

        var written = writer.WrittenMemory;
        Assert.Equal(16, written.Length);
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal((byte)i, written.Span[i]);
        }


        memory = writer.GetMemory(64);
        for (int i = 0; i < memory.Length; i++)
        {
            memory.Span[i] = (byte)(i + 16);
        }
        writer.Advance(64);

        written = writer.WrittenMemory;
        Assert.Equal(80, written.Length);
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal((byte)i, written.Span[i]);
        }


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.WrittenMemory);
    }

    [Fact]
    public void WrittenSpan()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Equal(0, writer.WrittenSpan.Length);

        var span = writer.GetSpan(16);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }
        writer.Advance(16);

        var written = writer.WrittenSpan;
        Assert.Equal(16, written.Length);
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal((byte)i, written[i]);
        }


        span = writer.GetSpan(64);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)(i + 16);
        }
        writer.Advance(64);

        written = writer.WrittenSpan;
        Assert.Equal(80, written.Length);
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal((byte)i, written[i]);
        }


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.WrittenSpan.Length);
    }

    [Fact]
    public void Ctor()
    {
        using var defaultCtor = new ArrayPoolBufferWriter<byte>();

        using var zero = new ArrayPoolBufferWriter<byte>(0);

        Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayPoolBufferWriter<byte>(-1));
    }

    [Fact]
    public void Advance()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(-1));
        writer.Advance(0);
        writer.Advance(64);
        Assert.Throws<ArgumentOutOfRangeException>(() => writer.Advance(1));


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.Advance(1));
    }

    [Fact]
    public void Clear()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        writer.Clear();
        Assert.Equal(0, writer.WrittenCount);

        var span = writer.GetSpan(16);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }
        writer.Advance(16);

        writer.Clear();
        Assert.Equal(0, writer.WrittenCount);
        writer.Advance(16);
        var written = writer.WrittenSpan;
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal(0, written[i]);
        }


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.Clear());
    }

    [Fact]
    public void Dispose()
    {
        var writer = new ArrayPoolBufferWriter<byte>();


        writer.Dispose();
        writer.Dispose();
    }

    [Fact]
    public void GetMemory()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        Assert.True(writer.GetMemory(16).Length >= 16);
        Assert.True(writer.GetMemory(128).Length >= 128);

        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetMemory(-1));


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.GetMemory(1));
    }

    [Fact]
    public void GetSpan()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        var span = writer.GetSpan(16);
        Assert.True(span.Length >= 16);

        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }
        writer.Advance(16);

        span = writer.GetSpan(128);
        Assert.True(span.Length >= 128);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }


        Assert.Throws<ArgumentOutOfRangeException>(() => writer.GetSpan(-1));


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan(1));
    }

    [Fact]
    public void ResetWrittenCount()
    {
        var writer = new ArrayPoolBufferWriter<byte>(64);

        writer.ResetWrittenCount();
        Assert.Equal(0, writer.WrittenCount);

        var span = writer.GetSpan(16);
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = (byte)i;
        }
        writer.Advance(16);

        writer.ResetWrittenCount();
        Assert.Equal(0, writer.WrittenCount);
        writer.Advance(16);
        var written = writer.WrittenSpan;
        for (int i = 0; i < written.Length; i++)
        {
            Assert.Equal((byte)i, written[i]);
        }


        writer.Dispose();
        Assert.Throws<ObjectDisposedException>(() => writer.ResetWrittenCount());
    }
}
