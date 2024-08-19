using System;
using System.Collections.Generic;
using System.IO;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Represents a type that can be used to read source text.
/// </summary>
internal interface ISourceReader
{
    /// <summary>
    /// True, if the reader has reached the end of the source text.
    /// </summary>
    public bool IsEnd { get; }

    /// <summary>
    /// The current position the reader is at.
    /// </summary>
    /// <exception cref="NotSupportedException">Thrown, if seeking is not supported by the given reader.</exception>
    public int Position { get; set; }

    /// <summary>
    /// Peeks ahead <paramref name="offset"/> amount of characters in the source without consuming anything.
    /// </summary>
    /// <param name="offset">The amount to look ahead from the current position.</param>
    /// <param name="default">The character to return, in case the peek would overrun the end of the source.</param>
    /// <returns>The character peeked <paramref name="offset"/> ahead, or <paramref name="default"/>,
    /// if it would cause an overrun.</returns>
    public char Peek(int offset = 0, char @default = '\0');

    /// <summary>
    /// Peeks ahead <paramref name="offset"/> amount of characters in the source without consuming anything.
    /// </summary>
    /// <param name="offset">The amount to look ahead from the current position.</param>
    /// <param name="result">The character at the peeked position.</param>
    /// <returns>False, if peek overran the end of source, otherwise true.</returns>
    public bool TryPeek(int offset, out char result);

    /// <summary>
    /// Advances <paramref name="amount"/> amount in the source.
    /// </summary>
    /// <param name="amount">The amount to advance.</param>
    /// <returns>The segment of <see cref="ReadOnlyMemory{char}"/> that contains the text that was skipped over.</returns>
    public ReadOnlyMemory<char> Advance(int amount = 1);
}

/// <summary>
/// Factory functions for constructing <see cref="ISourceReader"/>s.
/// </summary>
internal static class SourceReader
{
    private sealed class MemorySourceReader(ReadOnlyMemory<char> source) : ISourceReader
    {
        public bool IsEnd => this.Position >= source.Length;
        public int Position { get; set; }

        public ReadOnlyMemory<char> Advance(int amount = 1)
        {
            var result = source.Slice(this.Position, amount);
            this.Position += amount;
            return result;
        }

        public char Peek(int offset = 0, char @default = '\0') => this.Position + offset >= source.Length
            ? @default
            : source.Span[this.Position + offset];

        public bool TryPeek(int offset, out char result)
        {
            if (this.Position + offset < source.Length)
            {
                result = this.Peek(offset);
                return true;
            }
            else
            {
                result = default;
                return false;
            }
        }
    }

    private sealed class TextReaderSourceReader(TextReader reader) : ISourceReader
    {
        public bool IsEnd => !this.TryPeek(0, out _);
        public int Position { get; set; }

        private readonly List<char> peekBuffer = [];

        public ReadOnlyMemory<char> Advance(int amount = 1)
        {
            if (amount == 0) return ReadOnlyMemory<char>.Empty;

            this.TryPeek(amount - 1, out _);
            var result = this.peekBuffer.GetRange(0, amount).ToArray();
            this.peekBuffer.RemoveRange(0, amount);
            this.Position += amount;
            return result;
        }

        public char Peek(int offset = 0, char @default = '\0') => this.TryPeek(offset, out var result)
            ? result
            : @default;

        public bool TryPeek(int offset, out char result)
        {
            while (offset >= this.peekBuffer.Count)
            {
                var read = reader.Read();
                if (read == -1)
                {
                    result = default;
                    return false;
                }
                this.peekBuffer.Add((char)read);
            }
            result = this.peekBuffer[offset];
            return true;
        }
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

    /// <summary>
    /// Constructs an <see cref="ISourceReader"/> from a <see cref="TextReader"/>.
    /// </summary>
    /// <param name="reader">The <see cref="TextReader"/> to read from.</param>
    /// <returns>An <see cref="ISourceReader"/> reading from <paramref name="reader"/>.</returns>
    public static ISourceReader From(TextReader reader) => new TextReaderSourceReader(reader);
}
