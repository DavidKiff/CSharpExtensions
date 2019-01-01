using System;
using System.Collections.Generic;
using System.Linq;

namespace Extensions
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Randomise<T>(this IEnumerable<T> source)
        {
            var random = new Random();
            return source.OrderBy(_ => random.Next());
        }


        public static IEnumerable<T> Do<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
                yield return item;
            }
        }

        public static IEnumerable<T> Do<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var counter = 0;
            foreach (var item in enumerable)
            {
                action(item, counter++);
                yield return item;
            }
        }

        public static IDictionary<TKey, TValue> ToDeduplicatedDictionary<TKey, TValue>(this IEnumerable<TValue> enumerable, Func<TValue, TKey> keyFunction, bool replaceItem = false)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            foreach (var item in enumerable)
            {
                var key = keyFunction(item);
                if (replaceItem)
                {
                    dictionary[key] = item;
                }
                else if (!dictionary.ContainsKey(key))
                {
                    dictionary.Add(key, item);
                }
            }

            return dictionary;
        }

        public static IDictionary<TKey, TValue> ToDictionaryWithIndex<TKey, TValue, TItem>(this IEnumerable<TItem> enumerable, Func<TItem, int, TKey> keyFunction, Func<TItem, int, TValue> valueFunction)
        {
            var index = 0;
            return enumerable.ToDictionary(item => keyFunction(item, index), item => valueFunction(item, index++));
        }

        public static IEnumerable<TItem> Distinct<TKey, TItem>(this IEnumerable<TItem> enumerable, Func<TItem, TKey> keyFunction)
        {
            return enumerable.Distinct(new DelegateComparer<TKey, TItem>(keyFunction));
        }

        public static IEnumerable<TItem> Distinct<TKey, TItem>(IEnumerable<TItem> enumerable, Func<TItem, TKey> keyFunction, Action<TItem, int> foundDuplicateAction)
        {
            return enumerable.GroupBy(keyFunction)
                             .Select(group =>
                             {
                                 var first = group.First();
                                 var count = group.Count();

                                 if (count != 1)
                                 {
                                     foundDuplicateAction(first, count);
                                 }

                                 return first;
                             });
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
        {
            foreach (var item in enumerable)
            {
                action(item);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T, int> action)
        {
            var index = 0;
            foreach (var item in enumerable)
            {
                action(item, index++);
            }
        }

        public static IEnumerable<T> Concatenate<T>(this IEnumerable<T> enumerable, params T[] itemsToAdd)
        {
            return itemsToAdd == null ? enumerable : enumerable.Concat(itemsToAdd);
        }

        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> enumerable, params T[] itemsToPrepend)
        {
            return enumerable == null ? itemsToPrepend : itemsToPrepend.Concat(enumerable);
        }

        public static IEnumerable<IEnumerable<T>> Batch<T>(this IEnumerable<T> enumerable, int size)
        {
            T[] bucket = null;
            var index = 0;

            foreach (var item in enumerable ?? Enumerable.Empty<T>())
            {
                if (bucket == null)
                    bucket = new T[size];

                bucket[index++] = item;

                if (index != size) continue;

                yield return bucket;

                bucket = null;
                index = 0;
            }

            if (bucket != null && index > 0)
                yield return bucket.Take(index);
        }
        

        private sealed class DelegateComparer<TKey, TItem> : IEqualityComparer<TItem>
        {
            private readonly Func<TItem, TKey> _keyFunction;

            public DelegateComparer(Func<TItem, TKey> keyFunction)
            {
                _keyFunction = keyFunction;
            }

            public bool Equals(TItem x, TItem y)
            {
                return Equals(_keyFunction(x), _keyFunction(y));
            }

            public int GetHashCode(TItem obj)
            {
                return _keyFunction(obj).GetHashCode();
            }
        }
    }
}
