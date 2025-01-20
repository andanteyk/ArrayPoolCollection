namespace ArrayPoolCollection
{
    public static class LinqExtension
    {
        public static ArrayPoolWrapper<T> ToArrayPool<T>(this IEnumerable<T> source)
        {
            var segmentedArrayHeader = new SegmentedArray<T>.Stack16();
            using var segmentedArray = new SegmentedArray<T>(segmentedArrayHeader.AsSpan());
            segmentedArray.AddRange(source);

            var result = new ArrayPoolWrapper<T>(segmentedArray.GetTotalLength(), false);
            segmentedArray.WriteToSpan(result.AsSpan());

            return result;
        }
    }
}
