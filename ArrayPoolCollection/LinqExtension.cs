namespace ArrayPoolCollection
{
    public static class LinqExtension
    {
        public static ArrayPoolWrapper<T> ToArrayPool<T>(this IEnumerable<T> source)
        {
            if (CollectionHelper.TryGetSpan(source, out var span))
            {
                var result = new ArrayPoolWrapper<T>(span.Length, false);
                span.CopyTo(result);
                return result;
            }
            else if (CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                var result = new ArrayPoolWrapper<T>(count, false);

                if (source is ICollection<T> collection)
                {
                    result.CopyFrom(collection);
                }
                else
                {
                    int i = 0;
                    foreach (var element in source)
                    {
                        result[i] = element;
                        i++;
                    }
                }
                return result;
            }
            else
            {
                using var segmentedArray = new SegmentedArray<T>(SegmentedArray<T>.Stack16.Create().AsSpan());
                segmentedArray.AddRange(source);

                var result = new ArrayPoolWrapper<T>(segmentedArray.GetTotalLength(), false);
                segmentedArray.CopyTo(result.AsSpan());

                return result;
            }
        }


        public static ArrayPoolList<T> ToArrayPoolList<T>(this IEnumerable<T> source)
        {
            return new ArrayPoolList<T>(source);
        }


        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source)
            where TKey : notnull
        {
            return ToArrayPoolDictionary(source, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default);
        }

        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TKey, TValue>(this IEnumerable<(TKey key, TValue value)> source, IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 0;
            }
            var dict = new ArrayPoolDictionary<TKey, TValue>(count, comparer);

            foreach (var (key, value) in source)
            {
                dict.Add(key, value);
            }

            return dict;
        }

        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
            where TKey : notnull
        {
            return new(source);
        }

        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            return new(source, comparer);
        }

        public static ArrayPoolDictionary<TKey, TSource> ToArrayPoolDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            where TKey : notnull
        {
            return ToArrayPoolDictionary(source, keySelector, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default);
        }

        public static ArrayPoolDictionary<TKey, TSource> ToArrayPoolDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey>? comparer)
                    where TKey : notnull
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 0;
            }
            var dict = new ArrayPoolDictionary<TKey, TSource>(count, comparer);

            foreach (var element in source)
            {
                dict.Add(keySelector(element), element);
            }

            return dict;
        }

        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
            where TKey : notnull
        {
            return ToArrayPoolDictionary(source, keySelector, valueSelector, typeof(TKey).IsValueType ? null : EqualityComparer<TKey>.Default);
        }

        public static ArrayPoolDictionary<TKey, TValue> ToArrayPoolDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector, IEqualityComparer<TKey>? comparer)
            where TKey : notnull
        {
            if (!CollectionHelper.TryGetNonEnumeratedCount(source, out int count))
            {
                count = 0;
            }
            var dict = new ArrayPoolDictionary<TKey, TValue>(count, comparer);

            foreach (var element in source)
            {
                dict.Add(keySelector(element), valueSelector(element));
            }

            return dict;
        }


        public static ArrayPoolHashSet<T> ToArrayPoolHashSet<T>(this IEnumerable<T> source)
        {
            var set = new ArrayPoolHashSet<T>(source);
            return set;
        }

        public static ArrayPoolHashSet<T> ToArrayPoolHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer)
        {
            var set = new ArrayPoolHashSet<T>(source, comparer);
            return set;
        }
    }
}
