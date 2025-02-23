namespace ArrayPoolCollection.Pool
{
    internal struct BufferPoolStack<TBuffer>
    {
        public TBuffer?[] Array;
        public int Index;
        public int ReferenceCount;

        internal BufferPoolStack(int size)
        {
            Array = new TBuffer[size];
            Index = -1;
            ReferenceCount = 0;
        }

        public bool TryPop(out TBuffer array)
        {
            if (Index == -1)
            {
                array = default!;
                return false;
            }

            array = Array[Index]!;
            Array[Index] = default;
            Index--;
            ReferenceCount++;
            return true;
        }

        public bool TryPush(TBuffer array)
        {
            ReferenceCount++;

            if (Index < Array.Length - 1)
            {
                Index++;
                Array[Index] = array;
                return true;
            }
            return false;
        }

        public void ExpandBuffer(TBuffer?[]?[] reservedArrays)
        {
            int i;
            for (i = 0; i < reservedArrays.Length; i++)
            {
                if (reservedArrays[i]?.Length > Array.Length)
                {
                    Array.AsSpan().CopyTo(reservedArrays[i]);

                    var temp = Array;
                    Array = reservedArrays[i]!;
                    reservedArrays[i] = temp;
                    break;
                }
            }
            if (i == reservedArrays.Length)
            {
                for (i = 0; i < reservedArrays.Length; i++)
                {
                    if (reservedArrays[i] is null)
                    {
                        var newArray = new TBuffer[Array.Length << 1];
                        Array.AsSpan().CopyTo(newArray);
                        reservedArrays[i] = Array;
                        Array = newArray;
                        break;
                    }
                }
                if (i == reservedArrays.Length)
                {
                    var newArray = new TBuffer[Array.Length << 1];
                    Array.AsSpan().CopyTo(newArray);
                    Array = newArray;
                }
            }
        }

        public void Trim()
        {
            if (Array is null)
            {
                return;
            }

            if (ReferenceCount == 0)
            {
                for (int i = Index >> 1; i >= 0; i--)
                {
                    TryPop(out _);
                }
            }

            ReferenceCount = 0;
        }
    }
}
