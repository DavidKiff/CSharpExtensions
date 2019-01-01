using System;
using System.Reactive.Concurrency;
using Extensions;
using FluentAssertions;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class SchedulerExtensionsTests
    {
        [Test]
        public async void ScheduleIsAwaitable()
        {
            (await TaskPoolScheduler.Default.Schedule(() => "Hello")).Should().Be("Hello");
        }

        [Test]
        public async void ScheduleThrowsIfWorkThrows()
        {
            (await TaskPoolScheduler.Default.Schedule<string>(() => throw new Exception())).Should().Throws<Exception>();
        }
    }
}
