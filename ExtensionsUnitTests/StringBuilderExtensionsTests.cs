using System.Text;
using Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class StringBuilderExtensionsTests
    {
        [Test]
        public void WhenAllowsConditionalAppending()
        {
            new StringBuilder()
                .When(() => true, builder => builder.Append("Hello"))
                .ToString()
                .Should()
                .Be("Hello");
        }

        [Test]
        public void WhenDoesNotAppendWhenFalse()
        {
            new StringBuilder()
                .When(() => false, builder => builder.Append("Hello"))
                .ToString()
                .Should()
                .Be(string.Empty);
        }

        [Test]
        public void ProcessSequenceAppendsItems()
        {
            new StringBuilder()
                .ProcessSequence(new[] { 1, 2, 3 }, (builder, item) => builder.Append(item.ToString()))
                .Should()
                .Be("123");
        }
    }
}
