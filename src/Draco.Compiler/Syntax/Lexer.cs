using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Breaks up source code into a sequence of <see cref="IToken"/>s.
/// </summary>
internal sealed class Lexer
{
    /// <summary>
    /// The different kinds of modes the lexer can have.
    /// </summary>
    internal enum ModeKind
    {
        /// <summary>
        /// Regular source code lexing.
        /// </summary>
        Normal,

        /// <summary>
        /// Normal source code within string interpolation.
        /// </summary>
        Interpolation,

        /// <summary>
        /// Line string lexing.
        /// </summary>
        LineString,

        /// <summary>
        /// Multi-line string lexing.
        /// </summary>
        MultiLineString,
    }

    /// <summary>
    /// Represents a single mode on the mode stack.
    /// </summary>
    /// <param name="Kind">The kind of the mode.</param>
    /// <param name="ExtendedDelims">The number of extended delimiter characters needed for the current mode,
    /// in case it's a string lexing mode.</param>
    internal readonly record struct Mode(ModeKind Kind, int ExtendedDelims);

    /// <summary>
    /// The reader the source text is read from.
    /// </summary>
    public ISourceReader SourceReader { get; }

    private Mode CurrentMode => this.modeStack.Peek();

    private readonly Stack<Mode> modeStack = new();

    public Lexer(ISourceReader sourceReader)
    {
        this.SourceReader = sourceReader;
        this.PushMode(ModeKind.Normal, 0);
    }

    /// <summary>
    /// Reads the next <see cref="IToken"/> from the input.
    /// </summary>
    /// <returns>The <see cref="IToken"/> read.</returns>
    public IToken Next()
    {
        throw new NotImplementedException();
    }

    // Mode stack
    private void PushMode(ModeKind kind, int extendedDelims) => this.modeStack.Push(new(kind, extendedDelims));
    private void PopMode()
    {
        // We don't allow knocking off the lowest level mode
        if (this.modeStack.Count == 1) return;
        this.modeStack.Pop();
    }

    // Propagating functions to the source reader to decouple the API a bit, in case it changes
    // later for performance reasons
    private char Peek(int offset = 0, char @default = '\0') =>
        this.SourceReader.Peek(offset: offset, @default: @default);
    private ReadOnlyMemory<char> Advance(int amount = 1) => this.SourceReader.Advance(amount);

    // Character categorization
    private static bool IsIdent(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    private static bool IsSpace(char ch) => char.IsWhiteSpace(ch) && !IsNewline(ch);
    private static bool IsNewline(char ch) => ch == '\r' || ch == '\n';
    private static bool IsHexDigit(char ch) =>
           (ch >= '0' && ch <= '9')
        || (ch >= 'a' && ch <= 'f')
        || (ch >= 'A' && ch <= 'F');
}
