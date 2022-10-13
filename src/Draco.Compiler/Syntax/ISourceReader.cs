using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

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
    /// Advances <paramref name="amount"/> amount in the source.
    /// </summary>
    /// <param name="amount">The amount to advance.</param>
    /// <returns>The segment of <see cref="ReadOnlyMemory{char}"/> that contains the text that was skipped over.</returns>
    public ReadOnlyMemory<char> Advance(int amount = 1);
}
