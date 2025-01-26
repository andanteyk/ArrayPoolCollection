using System.Reflection;
using ArrayPoolCollection.Pool;

namespace ArrayPoolCollection.Tests
{
    [TestClass]
    public class ObjectPoolTests()
    {
        [TestMethod]
        public void Shared()
        {
            var random = ObjectPool<Random>.Shared.Rent();
            random.Next();
            ObjectPool<Random>.Shared.Return(random);
        }

        [TestMethod]
        public void Ctor()
        {
            using var pool = new ObjectPool<Random>(PooledObjectCallback.Create<Random>(), 256);
        }

        [TestMethod]
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

            Assert.AreEqual(1, instantiateCount);
            Assert.AreEqual(1024, rentCount);


            pool.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => pool.Rent());
        }

        [TestMethod]
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
            Assert.AreEqual(1024, returnCount);


            Assert.ThrowsException<ArgumentNullException>(() => pool.Return(default!));


            pool.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => pool.Return(default!));
        }

        [TestMethod]
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
            Assert.AreEqual(1024, instantiateCount);
            Assert.AreEqual(1024, returnCount);

            instantiateCount = 0;
            for (int i = 0; i < 1024; i++)
            {
                _ = pool.Rent();
            }
            Assert.AreEqual(0, instantiateCount);


            Assert.ThrowsException<ArgumentOutOfRangeException>(() => pool.Prewarm(-1));


            pool.Dispose();
            Assert.ThrowsException<ObjectDisposedException>(() => pool.Prewarm(10));
        }

        [TestMethod]
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
            Assert.AreEqual(1024 - 256, destroyCount);


            pool.Dispose();
            Assert.AreEqual(1024, destroyCount);
            // should not throw any exceptions
            pool.TrimExcess();
        }

        [TestMethod]
        public void Dispose()
        {
            var pool = new ObjectPool<Random>(PooledObjectCallback.Create<Random>());

            pool.Dispose();
            pool.Dispose();

            Assert.ThrowsException<InvalidOperationException>(() => ObjectPool<Random>.Shared.Dispose());
        }

        [TestMethod]
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
             ), 256);
            GabageCollectorCallback.Register(() =>
                pool.TrimExcess());

            pool.Prewarm(1024);

            GC.Collect(2);
            Thread.Sleep(10);

            Assert.AreEqual(1024 - 256, destroyCount);
        }
    }
}
