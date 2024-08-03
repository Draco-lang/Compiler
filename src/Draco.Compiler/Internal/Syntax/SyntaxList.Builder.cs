using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Syntax;

internal sealed partial class SyntaxList<TNode>
{
    /// <summary>
    /// The builder type for a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public sealed class Builder(ImmutableArray<TNode>.Builder underlying) : IList<TNode>
    {
        public bool IsReadOnly => false;
        public int Count => underlying.Count;

        public TNode this[int index]
        {
            get => underlying[index];
            set => underlying[index] = value;
        }

        public Builder()
            : this(ImmutableArray.CreateBuilder<TNode>())
        {
        }

        public SyntaxList<TNode> ToSyntaxList() => new(underlying.ToImmutable());
        public void Clear() => underlying.Clear();
        public void Add(TNode item) => underlying.Add(item);
        public void AddRange(IEnumerable<TNode> items) => underlying.AddRange(items);
        public void Insert(int index, TNode item) => underlying.Insert(index, item);
        public void InsertRange(int index, IEnumerable<TNode> items) => underlying.InsertRange(index, items);
        public bool Remove(TNode item) => underlying.Remove(item);
        public void RemoveAt(int index) => underlying.RemoveAt(index);
        public void RemoveRange(int index, int length) => underlying.RemoveRange(index, length);
        public bool Contains(TNode item) => underlying.Contains(item);
        public int IndexOf(TNode item) => underlying.IndexOf(item);
        public void CopyTo(TNode[] array, int arrayIndex) => underlying.CopyTo(array, arrayIndex);
        public IEnumerator<TNode> GetEnumerator() => underlying.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
