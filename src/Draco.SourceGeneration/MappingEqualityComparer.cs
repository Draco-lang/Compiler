using System;
using System.Collections.Generic;

namespace Draco.SourceGeneration;

internal static class MappingEqualityComparer
{
    internal static IEqualityComparer<TSource> Create<TSource, TKey>(Func<TSource, TKey> map)
        where TKey : IEquatable<TKey> => new MappingEqualityComparerImpl<TSource, TKey>(map);

    private sealed class MappingEqualityComparerImpl<TSource, TKey> : EqualityComparer<TSource>
        where TKey : IEquatable<TKey>
    {
        private readonly Func<TSource, TKey> map;

        internal MappingEqualityComparerImpl(Func<TSource, TKey> map)
        {
            this.map = map;
        }

        public override bool Equals(TSource x, TSource y) => this.map(x).Equals(this.map(y));
        public override int GetHashCode(TSource obj) => this.map(obj).GetHashCode();
    }
}
