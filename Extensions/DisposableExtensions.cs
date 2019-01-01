using System;

namespace Extensions
{
    public static class DisposableExtensions
    {
        public static TResult Using<TDisposable, TResult>(this TDisposable disposable, Func<TDisposable, TResult> func) where TDisposable : IDisposable
        {
            using (disposable)
            {
                return func(disposable);
            }
        }
    }
}
