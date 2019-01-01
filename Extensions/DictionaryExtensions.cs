using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            return dictionary.TryGetValue(key, out var foundItem) ? foundItem : defaultValue;
        }

        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> addFunction, Func<TValue, TValue> updateFunction)
        {
            if (dictionary.TryGetValue(key, out var found))
            {
                dictionary[key] = updateFunction(found);
            }
            else
            {
                dictionary.Add(key, addFunction());
            }
        }
    }
}
