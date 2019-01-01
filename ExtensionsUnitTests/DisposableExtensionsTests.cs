using System.Reactive.Disposables;
using Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class DisposableExtensionsTests
    {
        [Test]
        public void UsingCorrectlyCleansUp()
        {
            var myDisposable = new SingleAssignmentDisposable();

            myDisposable.Using(testDisposable => testDisposable.IsDisposed)
                        .Should()
                        .BeTrue();
        }
    }
}
