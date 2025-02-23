using System.Runtime.CompilerServices;

namespace ArrayPoolCollection.Pool
{
    public sealed class SlimBufferPool<TBuffer, TElement> : IBufferPool<TBuffer>, IDisposable
        where TBuffer : class
    {
        private readonly IBufferPoolPolicy<TBuffer, TElement> m_Policy;
        private BufferPoolStack<TBuffer>[]? m_Stacks;
        private readonly TBuffer?[]?[] m_ReservedArray;

        private const int SmallestBufferLengthExponent = 4;
        private const int BucketLength = 32 - SmallestBufferLengthExponent;

        private const int MinimumDefaultBucketSize = 16;
        private const int MaximumDefaultBucketSize = 1024;


        public SlimBufferPool(IBufferPoolPolicy<TBuffer, TElement> policy) : this(policy, true) { }

        public SlimBufferPool(IBufferPoolPolicy<TBuffer, TElement> policy, bool autoTrimWhenGabageCollected)
        {
            m_Policy = policy;
            m_Stacks = new BufferPoolStack<TBuffer>[BucketLength];
            m_ReservedArray = new TBuffer[BucketLength][];

            if (autoTrimWhenGabageCollected)
            {
                GabageCollectorCallback.Register(() => Trim());
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

            int bucketIndex = Math.Max(CollectionHelper.TrailingZeroCount((ulong)CollectionHelper.RoundUpToPowerOf2(span.Length)) - SmallestBufferLengthExponent, 0);

            if (m_Stacks[bucketIndex].Array is null)
            {
                m_Stacks[bucketIndex] = new BufferPoolStack<TBuffer>(Math.Max(MaximumDefaultBucketSize >> (bucketIndex >> 1), MinimumDefaultBucketSize));
            }

            if (!m_Stacks[bucketIndex].TryPush(value))
            {
                m_Stacks[bucketIndex].ExpandBuffer(m_ReservedArray);
                m_Stacks[bucketIndex].TryPush(value);
            }
        }

        public bool Trim()
        {
            if (m_Stacks is null)
            {
                return false;
            }

            foreach (var stacked in m_Stacks)
            {
                stacked.Trim();
            }
            return true;
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
        }
    }
}
