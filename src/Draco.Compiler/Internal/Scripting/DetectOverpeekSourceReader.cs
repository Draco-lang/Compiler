using System;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Internal.Scripting;

/// <summary>
/// A special source reader that flags overpeeking, meaning it will signal
/// if the parser tries to peek after the passed in end of source.
/// </summary>
internal sealed class DetectOverpeekSourceReader(ISourceReader underlying) : ISourceReader
{
    // We report false to encourage the parser to peek freely
    public bool IsEnd => false;

    public int Position
    {
        get => underlying.Position;
        set => underlying.Position = value;
    }

    /// <summary>
    /// True, if the parser has overpeeked.
    /// </summary>
    public bool HasOverpeeked { get; private set; }

    public ReadOnlyMemory<char> Advance(int amount = 1) => underlying.Advance(amount);

    public bool TryPeek(int offset, out char result)
    {
        if (!underlying.TryPeek(offset, out result))
        {
            this.HasOverpeeked = true;
            return false;
        }
        return true;
    }

    public char Peek(int offset = 0, char @default = '\0')
    {
        if (!this.TryPeek(offset, out var result))
        {
            this.HasOverpeeked = true;
            return @default;
        }
        return result;
    }
}
