using System.Collections.Generic;

namespace Extensions
{
    public sealed class Batch<T> : List<T>
    {
        public Batch(BatchType type, params T[] items) : base(items)
        {
            Type = type;
        }
        
        public BatchType Type { get; }
    }
}