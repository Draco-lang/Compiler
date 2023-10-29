using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Syntax;

internal sealed partial class SyntaxList<TNode>
{
    /// <summary>
    /// The builder type for a <see cref="SyntaxList{TNode}"/>.
    /// </summary>
    public sealed class Builder : IList<TNode>
    {
        public bool IsReadOnly => false;
        public int Count => this.builder.Count;

        public TNode this[int index]
        {
            get => this.builder[index];
            set => this.builder[index] = value;
        }

        private readonly ImmutableArray<TNode>.Builder builder;

        public Builder()
            : this(ImmutableArray.CreateBuilder<TNode>())
        {
        }

        public Builder(ImmutableArray<TNode>.Builder underlying)
        {
            this.builder = underlying;
        }

        public SyntaxList<TNode> ToSyntaxList() => new(this.builder.ToImmutable());
        public void Clear() => this.builder.Clear();
        public void Add(TNode item) => this.builder.Add(item);
        public void AddRange(IEnumerable<TNode> items) => this.builder.AddRange(items);
        public void Insert(int index, TNode item) => this.builder.Insert(index, item);
        public void InsertRange(int index, IEnumerable<TNode> items) => this.builder.InsertRange(index, items);
        public bool Remove(TNode item) => this.builder.Remove(item);
        public void RemoveAt(int index) => this.builder.RemoveAt(index);
        public void RemoveRange(int index, int length) => this.builder.RemoveRange(index, length);
        public bool Contains(TNode item) => this.builder.Contains(item);
        public int IndexOf(TNode item) => this.builder.IndexOf(item);
        public void CopyTo(TNode[] array, int arrayIndex) => this.builder.CopyTo(array, arrayIndex);
        public IEnumerator<TNode> GetEnumerator() => this.builder.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
