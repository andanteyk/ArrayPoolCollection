using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ArrayPoolCollection.Pool
{
    public sealed class DebugBufferPool<TBuffer, TElement> : IBufferPool<TBuffer>, IDisposable
        where TBuffer : class
    {
        private readonly IBufferPoolPolicy<TBuffer, TElement> m_Policy;
        private BufferPoolStack<TBuffer>[]? m_Stacks;
        private readonly ConditionalWeakTable<TBuffer, Tracer> m_Tracers;
        private readonly TaskScheduler m_Scheduler;
        private readonly TBuffer?[]?[] m_ReservedArray;

        private const int SmallestBufferLengthExponent = 4;
        private const int BucketLength = 32 - SmallestBufferLengthExponent;

        private const int MinimumDefaultBucketSize = 16;
        private const int MaximumDefaultBucketSize = 1024;


        public DebugBufferPool(IBufferPoolPolicy<TBuffer, TElement> policy) : this(policy, true) { }

        public DebugBufferPool(IBufferPoolPolicy<TBuffer, TElement> policy, bool autoTrimWhenGabageCollected)
        {
            m_Policy = policy;
            m_Stacks = new BufferPoolStack<TBuffer>[BucketLength];
            m_Tracers = new();
            m_Scheduler = TaskScheduler.Current;
            m_ReservedArray = new TBuffer[BucketLength][];

            if (autoTrimWhenGabageCollected)
            {
                GabageCollectorCallback.Register(() => TrimExcess());
            }
        }

        public TBuffer Rent(int minimumLength)
        {
            if (m_Stacks is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Stacks));
            }
            if ((uint)minimumLength > (uint)CollectionHelper.ArrayMaxLength)
            {
                ThrowHelper.ThrowArgumentOutOfRange(nameof(minimumLength), 0, CollectionHelper.ArrayMaxLength, minimumLength);
            }
            if (minimumLength == 0)
            {
                return m_Policy.Create(0);
            }

            int bucketIndex = Math.Max(CollectionHelper.TrailingZeroCount((ulong)CollectionHelper.RoundUpToPowerOf2(minimumLength)) - SmallestBufferLengthExponent, 0);
            TBuffer result;

            if (bucketIndex < BucketLength)
            {
                if (m_Stacks[bucketIndex].Array is null || !m_Stacks[bucketIndex].TryPop(out result))
                {
                    int newSize = 1 << (bucketIndex + SmallestBufferLengthExponent);
                    if (newSize < 0)
                    {
                        newSize = CollectionHelper.ArrayMaxLength;
                    }
                    result = m_Policy.Create(newSize);
                }
            }
            else
            {
                result = m_Policy.Create(minimumLength);
            }

            if (RuntimeHelpers.IsReferenceOrContainsReferences<TElement>())
            {
                m_Policy.AsSpan(result).Clear();
            }
            else
            {
                AsBytes(m_Policy.AsSpan(result)).Fill(0xcc);
            }

            m_Tracers.Add(result, new Tracer(this, Environment.StackTrace));

            return result;
        }

        public DisposableHandle<TBuffer> Rent(int minimumLength, out TBuffer result)
        {
            result = Rent(minimumLength);
            return new DisposableHandle<TBuffer>(result, this);
        }

        public void Return(TBuffer value)
        {
            Return(value, RuntimeHelpers.IsReferenceOrContainsReferences<TElement>());
        }

        public void Return(TBuffer value, bool clearArray)
        {
            if (m_Stacks is null)
            {
                ThrowHelper.ThrowObjectDisposed(nameof(m_Stacks));
            }

            var span = m_Policy.AsSpan(value);

            if (span.Length == 0)
            {
                return;
            }
            if (span.Length < 16 || (!CollectionHelper.IsPow2(span.Length) && span.Length != CollectionHelper.ArrayMaxLength))
            {
                ThrowHelper.ThrowIsNotPooledObject(nameof(value));
            }

            if (clearArray)
            {
                span.Clear();
            }
            else if (RuntimeHelpers.IsReferenceOrContainsReferences<TElement>())
            {
                span.Clear();
            }
            else
            {
                AsBytes(span).Fill(0xcc);
            }

            int bucketIndex = Math.Max(CollectionHelper.TrailingZeroCount((ulong)CollectionHelper.RoundUpToPowerOf2(span.Length)) - SmallestBufferLengthExponent, 0);

            if (m_Stacks[bucketIndex].Array is null)
            {
                m_Stacks[bucketIndex] = new BufferPoolStack<TBuffer>(Math.Max(MaximumDefaultBucketSize >> (bucketIndex >> 1), MinimumDefaultBucketSize));
            }

            for (int i = 0; i <= m_Stacks[bucketIndex].Index; i++)
            {
                var stacked = m_Stacks[bucketIndex].Array[i];

                if (ReferenceEquals(value, stacked))
                {
                    ThrowHelper.ThrowAlreadyReturned(nameof(value));
                }
            }

            if (m_Tracers.TryGetValue(value, out var tracer))
            {
                tracer.Dispose();
                m_Tracers.Remove(value);
            }
            else
            {
                ThrowHelper.ThrowIsNotPooledObject(nameof(value));
            }

            if (!m_Stacks[bucketIndex].TryPush(value))
            {
                m_Stacks[bucketIndex].ExpandBuffer(m_ReservedArray);
                m_Stacks[bucketIndex].TryPush(value);
            }
        }

        public bool TrimExcess()
        {
            if (m_Stacks is null)
            {
                return false;
            }

            foreach (var stacked in m_Stacks)
            {
                stacked.TrimExcess();
            }
            return true;
        }

        private static Span<byte> AsBytes(Span<TElement> span)
        {
            long longLength = (long)Unsafe.SizeOf<TElement>() * span.Length;
            int length = checked((int)longLength);
            ref var reference = ref Unsafe.As<TElement, byte>(ref MemoryMarshal.GetReference(span));
            var bytes = MemoryMarshal.CreateSpan(ref reference, length);

            return bytes;
        }

        /// <summary>
        /// Detects buffers that have been disposed without being Return()'d and throws an <see cref="ObjectDisposedException"/> if any.
        /// </summary>
        public void DetectLeaks()
        {
            ObjectDisposedException? ex = null;

            void detector(object? sender, UnobservedTaskExceptionEventArgs e)
            {
                if (e.Exception?.InnerException is ObjectDisposedException odex)
                {
                    e.SetObserved();
                    ex = odex;
                }
            }

            TaskScheduler.UnobservedTaskException += detector;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Here, the disposed buffers are collected and the detection task is started.

            GC.Collect();
            GC.WaitForPendingFinalizers();
            // Here, the detection task is collected and an UnobservedTaskException is fired.

            TaskScheduler.UnobservedTaskException -= detector;

            if (ex is not null)
            {
                throw ex;
            }
        }

        public void Dispose()
        {
            if (m_Stacks is not null)
            {
                foreach (var stack in m_Stacks)
                {
                    if (stack.Array is not null)
                    {
                        foreach (var element in stack.Array)
                        {
                            if (element is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                        }
                    }
                }
                m_Stacks = null;
            }

            foreach (var tracer in m_Tracers)
            {
                tracer.Value.Dispose();
            }
            m_Tracers.Clear();
        }


        private class Tracer : IDisposable
        {
            private readonly DebugBufferPool<TBuffer, TElement> m_Parent;
            private readonly string m_StackTrace;

            public Tracer(DebugBufferPool<TBuffer, TElement> parent, string stackTrace)
            {
                m_Parent = parent;
                m_StackTrace = stackTrace;
            }

            public void Dispose()
            {
                GC.SuppressFinalize(this);
            }

            ~Tracer()
            {
                // https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1065#finalizers

                var task = new Task(() =>
                {
                    ThrowHelper.ThrowPooledObjectDisposed(m_StackTrace);
                });
                task.Start(m_Parent.m_Scheduler);
                ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
            }
        }
    }
}
