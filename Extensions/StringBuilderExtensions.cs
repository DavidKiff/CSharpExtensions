using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Extensions
{
    public static class StringBuilderExtensions
    {
        public static StringBuilder When(this StringBuilder stringBuilder, Func<bool> predicate, Func<StringBuilder, StringBuilder> action)
        {
            return predicate() ? action(stringBuilder) : stringBuilder;
        }

        public static StringBuilder ProcessSequence<TItem>(this StringBuilder stringBuilder, IEnumerable<TItem> items, Func<StringBuilder, TItem, StringBuilder> function)
        {
            return items.Aggregate(stringBuilder, function);
        }
    }
}
