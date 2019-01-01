using System.Text;
using Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class FunctionalExtensionsTests
    {
        [Test]
        public void MapAllowsChaining()
        {
            new StringBuilder()
                    .AppendLine("Test")
                    .ToString()
                    .Map(Encoding.UTF8.GetBytes)
                    .Map(Encoding.UTF8.GetString)
                    .Should()
                    .Be("Test");
        }

        [Test]
        public void DoAllowsProcessingSideEffects()
        {
            var str = string.Empty;
            new StringBuilder()
                .AppendLine("Test")
                .ToString()
                .Do(s => str = s);

            str.Should().Be("Test");
        }
    }
}
