using System.Collections.Generic;
using System.Linq;
using Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace ExtensionsUnitTests
{
    internal sealed class EnumerableExtensionsTests
    {
        [Test]
        public void RandomiseReturnsDifferentOrder()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };

            var randomised = sequence.Randomise().ToList();

            randomised.SequenceEqual(sequence).Should().BeFalse();

            sequence.Randomise().SequenceEqual(randomised).Should().BeFalse();
        }

        [Test]
        public void DoAllowsSideEffects()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var state = new List<int>();

            sequence.Do(item => state.Add(item)).SequenceEqual(state).Should().BeTrue();
        }

        [Test]
        public void DoAllowsSideEffectsWithIndex()
        {
            var sequence = new[] { 1, 2, 3, 4, 5 };
            var state = new List<int>();

            int Add(int operand1, int operand2) => operand1 + operand2;

            sequence.Do((item, index) => state.Add(Add(item, index))).Select(Add).SequenceEqual(state).Should().BeTrue();
        }

        [Test]
        public void ToDeduplicatedDictionaryProvidesDictionaryWithoutDuplicates()
        {
            var dictionary = new [] { 1, 2, 2, 3, 4, 3 }.ToDeduplicatedDictionary(i => i);

            var expected = new[] { 1, 2, 3, 4 };
        
            dictionary.Select((kvp, index) => expected[index] == kvp.Key && expected[index] == kvp.Value)
                      .All(b => b)
                      .Should()
                      .BeTrue();
        }

        [Test]
        public void DistinctReturnsDistinctItems()
        {
            new[] { 1, 2, 2, 3, 4, 1 }.Distinct(item => item).SequenceEqual(new [] { 1, 2, 3, 4 } ).Should().BeTrue();
        }
    }
}