using System;

namespace Extensions
{
    public static class FunctionalExtensions
    {
        public static TResult Map<TSource, TResult>(this TSource source, Func<TSource, TResult> func) => func(source);

        public static TSource Do<TSource>(this TSource source, Action<TSource> action)
        {
            action(source);
            return source;
        }
    }
}
