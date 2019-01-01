using System;
using System.Collections.Generic;

namespace Extensions
{
    public static class CollectionExtensions
    {
        public static void DisposeAndClear<T>(this ICollection<T> enumerable) where T : IDisposable
        {
            if (enumerable == null) return;

            enumerable.ForEach(item => item.Dispose());
            enumerable.Clear();
        }
    }
}
