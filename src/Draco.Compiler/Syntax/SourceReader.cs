using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Factory functions for constructing <see cref="ISourceReader"/>s.
/// </summary>
public static class SourceReader
{
    private sealed class MemorySourceReader : ISourceReader
    {
        public bool IsEnd => this.Position >= this.source.Length;
        public int Position { get; set; }

        private readonly ReadOnlyMemory<char> source;

        public MemorySourceReader(ReadOnlyMemory<char> source)
        {
            this.source = source;
        }

        public ReadOnlyMemory<char> Advance(int amount = 1)
        {
            var result = this.source.Slice(this.Position, amount);
            this.Position += amount;
            return result;
        }

        public char Peek(int offset = 0, char @default = '\0') => this.Position + offset >= this.source.Length
            ? @default
            : this.source.Span[this.Position + offset];
    }

    /// <summary>
    /// Constructs an <see cref="ISourceReader"/> from a <see cref="ReadOnlyMemory{char}"/>.
    /// </summary>
    /// <param name="source">The source text as a <see cref="ReadOnlyMemory{char}"/>.</param>
    /// <returns>An <see cref="ISourceReader"/> reading <paramref name="source"/>.</returns>
    public static ISourceReader From(ReadOnlyMemory<char> source) => new MemorySourceReader(source);

    /// <summary>
    /// Constructs an <see cref="ISourceReader"/> from a <see cref="string"/>.
    /// </summary>
    /// <param name="source">The source text as a <see cref="string"/>.</param>
    /// <returns>An <see cref="ISourceReader"/> reading <paramref name="source"/>.</returns>
    public static ISourceReader From(string source) => new MemorySourceReader(source.AsMemory());
}
