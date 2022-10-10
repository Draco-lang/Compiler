using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Syntax;

/// <summary>
/// Breaks up source code into a sequence of <see cref="Token"/>s.
/// </summary>
public sealed class Lexer
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
    /// Reads the next <see cref="Token"/> from the input.
    /// </summary>
    /// <returns>The <see cref="Token"/> read.</returns>
    public Token Next()
    {
        // Mostly mode-neutral things
        var mode = this.CurrentMode;

        Token TakeNewline(int length)
        {
            switch (mode.Kind)
            {
            case ModeKind.Normal:
            case ModeKind.Interpolation:
                return this.Take(TokenType.Newline, length);

            case ModeKind.LineString:
                // We also pop off the string mode, not to infect the rest of the input
                this.PopMode();
                return this.Take(TokenType.Newline, length);

            case ModeKind.MultiLineString:
                return this.Take(TokenType.StringNewline, length);

            default:
                throw new InvalidOperationException();
            }
        }

        // End of input
        if (this.SourceReader.IsEnd) return new(TokenType.EndOfInput, ReadOnlyMemory<char>.Empty);

        var ch = this.Peek();

        // Newlines
        if (ch == '\r')
        {
            // Windows-style newline
            if (this.Peek(1) == '\n') return this.Take(TokenType.Newline, 2);
            // OS-X 9-style newline
            return TakeNewline(1);
        }
        if (ch == '\n')
        {
            // UNIX-style newline
            return TakeNewline(1);
        }

        // Mode-specific
        if (mode.Kind == ModeKind.Normal || mode.Kind == ModeKind.Interpolation)
        {
            // Whitespace
            if (IsSpace(ch))
            {
                // We merge it into one chunk to not produce so many individual tokens
                var offset = 1;
                for (; IsSpace(this.Peek(offset)); ++offset) ;
                return this.Take(TokenType.Whitespace, offset);
            }

            // Line-comment
            if (ch == '/' && this.Peek(1) == '/')
            {
                var offset = 2;
                // NOTE: We use a little trick here, we specify a newline character as the default for Peek,
                // which means that this will terminate, even if the comment was on the last line of the file
                // without a line break
                for (; !IsNewline(this.Peek(offset, @default: '\n')); ++offset) ;
                return this.Take(TokenType.LineComment, offset);
            }

            // Punctuation
            switch (ch)
            {
            case '(': return this.Take(TokenType.ParenOpen, 1);
            case ')': return this.Take(TokenType.ParenClose, 1);
            case '{':
                if (mode.Kind == ModeKind.Interpolation) this.PushMode(ModeKind.Interpolation, 0);
                return this.Take(TokenType.CurlyOpen, 1);
            case '}':
                if (mode.Kind == ModeKind.Interpolation)
                {
                    this.PopMode();
                    // If we are not in interpolation anymore, this is an interpolation end token
                    if (this.CurrentMode.Kind != ModeKind.Interpolation) return this.Take(TokenType.InterpolationEnd, 1);
                }
                return this.Take(TokenType.CurlyClose, 1);
            case '[': return this.Take(TokenType.BracketOpen, 1);
            case ']': return this.Take(TokenType.BracketClose, 1);

            case '.': return this.Take(TokenType.Dot, 1);
            case ',': return this.Take(TokenType.Comma, 1);
            case ':': return this.Take(TokenType.Colon, 1);
            case ';': return this.Take(TokenType.Semicolon, 1);
            }

            // Numeric literals
            // NOTE: We check for numeric literals first, so we can be lazy with the identifier checking later
            // Since digits would be a valid identifier character, we can avoid separating the check for the
            // first character
            if (char.IsDigit(ch))
            {
                var offset = 1;
                for (; char.IsDigit(this.Peek(offset)); ++offset) ;
                return this.Take(TokenType.LiteralInteger, offset);
            }

            // Identifier-like tokens
            if (IsIdent(ch))
            {
                var offset = 1;
                for (; IsIdent(this.Peek(offset)); ++offset) ;
                var token = this.Take(TokenType.LiteralInteger, offset);
                // Remap keywords
                // TODO: Any better/faster way?
                var newTokenType = token.Text switch
                {
                    var _ when token.Text.Span.SequenceEqual("from") => TokenType.KeywordFrom,
                    var _ when token.Text.Span.SequenceEqual("func") => TokenType.KeywordFunc,
                    var _ when token.Text.Span.SequenceEqual("import") => TokenType.KeywordImport,
                    var _ when token.Text.Span.SequenceEqual("return") => TokenType.KeywordReturn,
                    var _ when token.Text.Span.SequenceEqual("if") => TokenType.KeywordIf,
                    var _ when token.Text.Span.SequenceEqual("else") => TokenType.KeywordElse,
                    var _ when token.Text.Span.SequenceEqual("var") => TokenType.KeywordVar,
                    var _ when token.Text.Span.SequenceEqual("val") => TokenType.KeywordVal,
                    var _ when token.Text.Span.SequenceEqual("goto") => TokenType.KeywordGoto,
                    var _ when token.Text.Span.SequenceEqual("while") => TokenType.KeywordWhile,
                    _ => TokenType.Identifier,
                };
                return new(newTokenType, token.Text);
            }

            // String literal starts
            {
                var extendedDelims = 0;
                for (; this.Peek(extendedDelims) == '#'; ++extendedDelims) ;
                var offset = extendedDelims;
                if (this.Peek(offset) == '"')
                {
                    if (this.Peek(offset + 1) == '"' && this.Peek(offset + 2) == '"')
                    {
                        // Mutli-line string opening quotes
                        this.PushMode(ModeKind.MultiLineString, extendedDelims);
                        return this.Take(TokenType.MultiLineStringStart, offset + 3);
                    }
                    // Single-line string opening quote
                    this.PushMode(ModeKind.LineString, extendedDelims);
                    return this.Take(TokenType.LineStringStart, offset + 1);
                }
            }

            // Unknown
            return this.Take(TokenType.Unknown, 1);
        }
        else if (mode.Kind == ModeKind.LineString || mode.Kind == ModeKind.MultiLineString)
        {
            // Some kind of string mode

            if (ch == '"')
            {
                // We are potentially closing a string here
                var offset = 0;
                if (mode.Kind == ModeKind.MultiLineString)
                {
                    // We are expecting 2 more quotes
                    if (this.Peek(1) != '"' || this.Peek(2) != '"') goto not_string_end;
                    offset = 3;
                }
                else
                {
                    // Just a line string
                    offset = 1;
                }
                // Count the number of required closing delimiters
                for (var i = 0; i < mode.ExtendedDelims; ++i)
                {
                    if (this.Peek(offset + i) != '#') goto not_string_end;
                }
                offset += mode.ExtendedDelims;
                // Hit the end of the string
                this.PopMode();
                var tokenType = mode.Kind == ModeKind.LineString
                    ? TokenType.LineStringEnd
                    : TokenType.MultiLineStringEnd;
                return this.Take(tokenType, offset);
            }

        not_string_end:
            if (ch == '\\')
            {
                // Potential escape sequence
                var offset = 1;
                // Count the number of required delimiters
                for (var i = 0; i < mode.ExtendedDelims; ++i)
                {
                    if (this.Peek(offset + i) != '#') goto not_escape_sequence;
                }
                offset += mode.ExtendedDelims;
                // Some kind of escape
                var esc = this.Peek(offset);
                // Line continuations are only available in multi-line strings
                if (mode.Kind == ModeKind.MultiLineString)
                {
                    if (esc == '\r')
                    {
                        // It's a line-continuation, either Windows or OS-X 9-style
                        if (this.Peek(offset + 1) == '\n')
                        {
                            // Windows
                            return this.Take(TokenType.EscapeSequence, offset + 2);
                        }
                        // OS-X 9
                        return this.Take(TokenType.EscapeSequence, offset + 1);
                    }
                    if (esc == '\n')
                    {
                        // It's a line-continuation, UNIX-style
                        return this.Take(TokenType.EscapeSequence, offset + 1);
                    }
                }
                // Valid in any string
                if (esc == 'u' && this.Peek(offset + 1) == '{')
                {
                    // Unicode codepoint specified in braces
                    offset += 2;
                    for (; IsHexDigit(this.Peek(offset)); ++offset) ;
                    // Consume closing brace
                    if (this.Peek(offset) == '}') ++offset;
                    return this.Take(TokenType.EscapeSequence, offset);
                }
                if (esc == '{')
                {
                    // Interpolation start
                    this.PushMode(ModeKind.Interpolation, 0);
                    return this.Take(TokenType.InterpolationStart, offset + 1);
                }
                // Any single-character escape
                return this.Take(TokenType.EscapeSequence, offset + 1);
            }

        not_escape_sequence:
            if (IsStringContent(ch))
            {
                var offset = 1;
                for (; IsStringContent(this.Peek(offset)); ++offset) ;
                return this.Take(TokenType.StringContent, offset);
            }

            // Anything else is categorized as a single-character string content
            return this.Take(TokenType.StringContent, 1);
        }
        else
        {
            throw new NotImplementedException("Unimplemented lexer mode");
        }
    }

    // Mode stack
    private void PushMode(ModeKind kind, int extendedDelims) => this.modeStack.Push(new(kind, extendedDelims));
    private void PopMode()
    {
        // We don't allow knocking off the lowest level mode
        if (this.modeStack.Count == 1) return;
        this.modeStack.Pop();
    }

    // Utility for token construction
    private Token Take(TokenType tokenType, int length) => new(tokenType, this.Advance(length));

    // Propagating functions to the source reader to decouple the API a bit, in case it changes
    // later for performance reasons
    private char Peek(int offset = 0, char @default = '\0') =>
        this.SourceReader.Peek(offset: offset, @default: @default);
    private ReadOnlyMemory<char> Advance(int amount = 1) => this.SourceReader.Advance(amount);

    // Character categorization
    private static bool IsIdent(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    private static bool IsSpace(char ch) => char.IsWhiteSpace(ch) && !IsNewline(ch);
    private static bool IsNewline(char ch) => ch == '\r' || ch == '\n';
    private static bool IsStringContent(char ch) =>
           !IsNewline(ch)
        && !char.IsControl(ch)
        && ch != '"'
        && ch != '\\';
    private static bool IsHexDigit(char ch) =>
           (ch >= '0' && ch <= '9')
        || (ch >= 'a' && ch <= 'f')
        || (ch >= 'A' && ch <= 'F');
}
