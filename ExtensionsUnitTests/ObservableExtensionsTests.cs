using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using FluentAssertions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class ObservableExtensionsTests
    {
        [Test]
        public async void ReplayOneDoesNotCacheException()
        {
            // The normal ReplaySubject caches the exception too, so retries will always throw.  This method avoids that issue. 

            var hasThrown = false;
            var stream = Observable.Create<int>(observer =>
            {
                if (!hasThrown)
                {
                    observer.OnNext(1);
                    hasThrown = true;
                    observer.OnError(new Exception("Booom!"));
                }
                else
                {
                    observer.OnNext(2);
                }
                return Disposable.Empty;
            });

            (await stream.Retry(1).Take(2).ToList()).SequenceEqual(new [] { 1, 2 }).Should().BeTrue();
        }
    }
}
