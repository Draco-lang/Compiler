using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

    // Mode stack of the lexer, this is actual, changing state
    private readonly Stack<Mode> modeStack = new();

    // These are various cached builder types to save on allocations
    // The important thing is that these should not carry any significant state in the lexer in any way,
    // meaning that the behavior should be identical if we reallocated/cleared these before each token
    private readonly StringBuilder valueBuilder = new();
    private readonly ImmutableArray<IToken>.Builder leadingTriviaList = ImmutableArray.CreateBuilder<IToken>();
    private readonly ImmutableArray<IToken>.Builder trailingTriviaList = ImmutableArray.CreateBuilder<IToken>();

    public Lexer(ISourceReader sourceReader)
    {
        this.SourceReader = sourceReader;
        this.PushMode(ModeKind.Normal, 0);
    }

    /// <summary>
    /// Reads the next <see cref="IToken"/> from the input.
    /// </summary>
    /// <returns>The <see cref="IToken"/> read.</returns>
    public IToken Lex()
    {
        this.valueBuilder.Clear();
        switch (this.CurrentMode.Kind)
        {
        case ModeKind.Normal:
        case ModeKind.Interpolation:
        {
            // Normal tokens can have trivia
            this.ParseLeadingTriviaList();
            var token = this.LexNormal();
            if (token.Type != TokenType.InterpolationEnd) this.ParseTrailingTriviaList();
            // If there was any leading or trailing trivia, we have to re-map
            if (this.leadingTriviaList.Count > 0 || this.trailingTriviaList.Count > 0)
            {
                return token.AddTrivia(
                    new(this.leadingTriviaList.ToImmutable()),
                    new(this.trailingTriviaList.ToImmutable()));
            }
            else
            {
                // Nothing to do
                return token;
            }
        }

        case ModeKind.LineString:
        case ModeKind.MultiLineString:
            return this.LexString();

        default:
            throw new InvalidOperationException("unsupported lexer mode");
        }
    }

    private IToken LexNormal()
    {
        IToken TakeBasic(TokenType tokenType, int length)
        {
            this.Advance(length);
            return IToken.From(tokenType);
        }

        IToken TakeWithText(TokenType tokenType, int length) =>
            IToken.From(tokenType, this.AdvanceWithText(length));

        // First check for end of source here
        if (this.SourceReader.IsEnd) return IToken.From(TokenType.EndOfInput);

        var modeKind = this.CurrentMode.Kind;
        var ch = this.Peek();

        // Punctuation
        switch (ch)
        {
        case '(': return TakeBasic(TokenType.ParenOpen, 1);
        case ')': return TakeBasic(TokenType.ParenClose, 1);
        case '{':
            if (modeKind == ModeKind.Interpolation) this.PushMode(ModeKind.Interpolation, 0);
            return TakeBasic(TokenType.CurlyOpen, 1);
        case '}':
            if (modeKind == ModeKind.Interpolation)
            {
                this.PopMode();
                // If we are not in interpolation anymore, this is an interpolation end token
                if (this.CurrentMode.Kind != ModeKind.Interpolation) return TakeBasic(TokenType.InterpolationEnd, 1);
            }
            return TakeBasic(TokenType.CurlyClose, 1);
        case '[': return TakeBasic(TokenType.BracketOpen, 1);
        case ']': return TakeBasic(TokenType.BracketClose, 1);

        case '.': return TakeBasic(TokenType.Dot, 1);
        case ',': return TakeBasic(TokenType.Comma, 1);
        case ':': return TakeBasic(TokenType.Colon, 1);
        case ';': return TakeBasic(TokenType.Semicolon, 1);
        case '+':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.PlusAssign, 2);
            return TakeBasic(TokenType.Plus, 1);
        case '-':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.MinusAssign, 2);
            return TakeBasic(TokenType.Minus, 1);
        case '*':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.StarAssign, 2);
            return TakeBasic(TokenType.Star, 1);
        case '/':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.SlashAssign, 2);
            return TakeBasic(TokenType.Slash, 1);
        case '<':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.LessEqual, 2);
            return TakeBasic(TokenType.LessThan, 1);
        case '>':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.GreaterEqual, 2);
            return TakeBasic(TokenType.GreaterThan, 1);
        case '=':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.Equal, 2);
            return TakeBasic(TokenType.Assign, 1);
        case '!':
            if (this.Peek(1) == '=') return TakeBasic(TokenType.NotEqual, 2);
            // NOTE: '!' in it self is not negation!
            break;
        }

        // Numeric literals
        // NOTE: We check for numeric literals first, so we can be lazy with the identifier checking later
        // Since digits would be a valid identifier character, we can avoid separating the check for the
        // first character
        if (char.IsDigit(ch))
        {
            var offset = 1;
            while (char.IsDigit(this.Peek(offset))) ++offset;
            var view = this.Advance(offset);
            // TODO: Parsing into an int32 might not be the best idea
            var value = int.Parse(view.Span);
            return IToken.From(TokenType.LiteralInteger, view.ToString(), value);
        }

        // Identifier-like tokens
        if (IsIdent(ch))
        {
            var offset = 1;
            while (IsIdent(this.Peek(offset))) ++offset;
            var ident = this.Advance(offset);
            // Remap keywords
            // TODO: Any better/faster way?
            var tokenType = ident switch
            {
                var _ when ident.Span.SequenceEqual("and") => TokenType.KeywordAnd,
                var _ when ident.Span.SequenceEqual("else") => TokenType.KeywordElse,
                var _ when ident.Span.SequenceEqual("false") => TokenType.KeywordFalse,
                var _ when ident.Span.SequenceEqual("from") => TokenType.KeywordFrom,
                var _ when ident.Span.SequenceEqual("func") => TokenType.KeywordFunc,
                var _ when ident.Span.SequenceEqual("goto") => TokenType.KeywordGoto,
                var _ when ident.Span.SequenceEqual("if") => TokenType.KeywordIf,
                var _ when ident.Span.SequenceEqual("import") => TokenType.KeywordImport,
                var _ when ident.Span.SequenceEqual("mod") => TokenType.KeywordMod,
                var _ when ident.Span.SequenceEqual("not") => TokenType.KeywordNot,
                var _ when ident.Span.SequenceEqual("or") => TokenType.KeywordOr,
                var _ when ident.Span.SequenceEqual("rem") => TokenType.KeywordRem,
                var _ when ident.Span.SequenceEqual("return") => TokenType.KeywordReturn,
                var _ when ident.Span.SequenceEqual("true") => TokenType.KeywordTrue,
                var _ when ident.Span.SequenceEqual("val") => TokenType.KeywordVal,
                var _ when ident.Span.SequenceEqual("var") => TokenType.KeywordVar,
                var _ when ident.Span.SequenceEqual("while") => TokenType.KeywordWhile,
                _ => TokenType.Identifier,
            };
            // Return the appropriate token
            if (tokenType == TokenType.Identifier)
            {
                // Need to save the value
                return IToken.From(TokenType.Identifier, ident.ToString());
            }
            else
            {
                // Keyword, we save on allocation
                return IToken.From(tokenType);
            }
        }

        // Character literals
        if (ch == '\'')
        {
            var offset = 1;
            var ch2 = this.Peek(offset);
            var resultChar = '\0';
            if (ch2 == '\\')
            {
                // Escape-sequence
                ++offset;
                resultChar = this.ParseEscapeSequence(ref offset);
            }
            else if (!char.IsControl(ch2))
            {
                // Regular character
                resultChar = ch2;
                ++offset;
            }
            else
            {
                // TODO: Error, invalid in character literal
                throw new NotImplementedException();
            }
            // Expect closing quote
            if (this.Peek(offset) == '\'')
            {
                ++offset;
            }
            else
            {
                // TODO: Error, unclosed character literal
                throw new NotImplementedException();
            }
            // Done
            var text = this.AdvanceWithText(offset);
            return IToken.From(TokenType.LiteralCharacter, text, resultChar);
        }

        // String literal starts
        {
            var extendedDelims = 0;
            while (this.Peek(extendedDelims) == '#') ++extendedDelims;
            var offset = extendedDelims;
            if (this.Peek(offset) == '"')
            {
                if (this.Peek(offset + 1) == '"' && this.Peek(offset + 2) == '"')
                {
                    // Mutli-line string opening quotes
                    this.PushMode(ModeKind.MultiLineString, extendedDelims);
                    return TakeWithText(TokenType.MultiLineStringStart, offset + 3);
                }
                // Single-line string opening quote
                this.PushMode(ModeKind.LineString, extendedDelims);
                return TakeWithText(TokenType.LineStringStart, offset + 1);
            }
        }

        // Unknown
        return TakeWithText(TokenType.Unknown, 1);
    }

    private IToken LexString()
    {
        // First check for end of source here
        if (this.SourceReader.IsEnd) return IToken.From(TokenType.EndOfInput);

        // Get the largest continuous sequence without linebreaks or interpolation
        var mode = this.CurrentMode;
        var offset = 0;

    start:
        var ch = this.Peek(offset);

        // Check for closing quotes
        if (ch == '"')
        {
            var endLength = 0;
            if (mode.Kind == ModeKind.MultiLineString)
            {
                // We are expecting 2 more quotes
                if (this.Peek(offset + 1) != '"' || this.Peek(offset + 2) != '"') goto not_string_end;
                endLength = 3;
            }
            else
            {
                // Just a line string
                endLength = 1;
            }
            // Count the number of required closing delimiters
            for (var i = 0; i < mode.ExtendedDelims; ++i)
            {
                if (this.Peek(offset + endLength + i) != '#') goto not_string_end;
            }
            endLength += mode.ExtendedDelims;
            // Hit the end of the string
            // NOTE: Since we are lexing the end of token as a separate character, we'll simply return what's
            // been lexed so far without the closing quotes, the next lex call will actually lex the closing
            // quotes.
            if (offset == 0)
            {
                // Nothing lexed yet, we can return the end of string token
                this.PopMode();
                var tokenType = mode.Kind == ModeKind.LineString
                    ? TokenType.LineStringEnd
                    : TokenType.MultiLineStringEnd;
                return IToken.From(tokenType, this.AdvanceWithText(endLength));
            }
            else
            {
                // This will only be the end of string in the next iteration, we just return what we have
                // consumed so far
                return IToken.From(TokenType.StringContent, this.AdvanceWithText(offset), this.valueBuilder.ToString());
            }
        }

    not_string_end:
        // Check for escape sequence
        if (ch == '\\')
        {
            // Count the number of required delimiters
            for (var i = 0; i < mode.ExtendedDelims; ++i)
            {
                if (this.Peek(offset + i + 1) != '#') goto not_escape_sequence;
            }

            // Interpolation
            if (this.Peek(offset + mode.ExtendedDelims + 1) == '{')
            {
                // Nothing lexed yet,we can return the start of interpolation token
                if (offset == 0)
                {
                    this.PushMode(ModeKind.Interpolation, 0);
                    return IToken.From(TokenType.InterpolationStart, this.AdvanceWithText(mode.ExtendedDelims + 2));
                }
                else
                {
                    // This will only be interpolation in the next iteration, we just return what we have
                    // consumed so far
                    return IToken.From(TokenType.StringContent, this.AdvanceWithText(offset), this.valueBuilder.ToString());
                }
            }
            offset += mode.ExtendedDelims + 1;
            // Line continuation           
            ch = this.Peek(offset);
            if (char.IsWhiteSpace(ch))
            {
                var whiteCharOffset = 0;
                while (char.IsWhiteSpace(ch) && ch != '\n')
                {
                    whiteCharOffset++;
                    ch = this.Peek(offset + whiteCharOffset);
                }
                if (this.TryParseNewline(offset + whiteCharOffset, out int length))
                {
                    //TODO: decide what to do if there are whitespaces after line continuation
                    offset += whiteCharOffset + length;
                    goto start;
                }
                else
                {
                    // TODO: Error, there is not newline directly after line continuation
                    throw new NotImplementedException();
                }
            }
            // Try to parse an escape
            var escaped = this.ParseEscapeSequence(ref offset);
            // Append to result
            this.valueBuilder.Append(escaped);
            goto start;
        }

    not_escape_sequence:
        // Check for newline
        if (this.TryParseNewline(offset, out var newlineLength))
        {
            if (mode.Kind == ModeKind.LineString)
            {
                // TODO: Report error, pop mode, return the last bits of content, if any
                // DO NOT CONSUME THIS NEWLINE
                throw new NotImplementedException();
            }
            else
            {
                // Newlines are completely valid in multiline stirngs
                for (var i = 0; i < newlineLength; ++i) this.valueBuilder.Append(this.Peek(offset + i));
                offset += newlineLength;
                goto start;
            }
        }

        // Just consume as a content character
        this.valueBuilder.Append(ch);
        ++offset;
        goto start;
    }

    /// <summary>
    /// Parses an escape sequence for strings and character literals.
    /// Reports an error, if the escape sequence is illegal.
    /// </summary>
    /// <param name="offset">A reference to an offset that points after the backslash and optional extended
    /// delimiter characters. If parsing the escape succeeded, the result is written back here.</param>
    /// <param name="result">The resulting character value gets written here, if the escape was parsed
    /// successfully.</param>
    /// <returns>True, if an escape was successfully parsed.</returns>
    private char ParseEscapeSequence(ref int offset)
    {
        var esc = this.Peek(offset);
        // Valid in any string
        if (esc == 'u' && this.Peek(offset + 1) == '{')
        {
            offset += 2;
            // Parse hex unicode value
            var unicodeValue = 0;
            var length = 0;
            while (TryParseHexDigit(this.Peek(offset), out var digit))
            {
                unicodeValue = unicodeValue * 16 + digit;
                ++length;
                ++offset;
            }
            // Expect closing brace
            if (this.Peek(offset) == '}')
            {
                ++offset;
                if (length > 0)
                {
                    // TODO: This doesn't exactly look efficient or fool-proof
                    // Find out why this returns a string
                    return char.ConvertFromUtf32(unicodeValue)[0];
                }
                else
                {
                    // TODO: Error, zero-length unicode codepoint
                    throw new NotImplementedException();
                }
            }
            else
            {
                // TODO: Error, expected closing brace
                throw new NotImplementedException();
            }
        }
        // Any single-character escape, find the escaped equivalent
        char? escaped = esc switch
        {
            '0'  => '\0',
            'a'  => '\a',
            'b'  => '\b',
            'f'  => '\f',
            'n'  => '\n',
            'r'  => '\r',
            't'  => '\t',
            'v'  => '\v',
            '\'' => '\'',
            '\"' => '\"',
            _ => null,
        };
        if (escaped is not null)
        {
            ++offset;
            return escaped.Value;
        }
        else
        {
            // TODO: Error, unknown escape
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Parses leading trivia into <see cref="leadingTriviaList"/>. Leading trivia is essentially all trivia
    /// tokens until the next significant token.
    /// </summary>
    private void ParseLeadingTriviaList()
    {
        this.leadingTriviaList.Clear();
        while (this.TryParseTrivia(out var trivia)) this.leadingTriviaList.Add(trivia);
    }

    /// <summary>
    /// Parses trailing trivia into <see cref="trailingTriviaList"/>. Trailing trivia is essentially all trivia
    /// tokens until the next end of line, which is also included in the list.
    /// </summary>
    private void ParseTrailingTriviaList()
    {
        this.trailingTriviaList.Clear();
        while (this.TryParseTrivia(out var trivia))
        {
            this.trailingTriviaList.Add(trivia);
            if (trivia.Type == TokenType.Newline) break;
        }
    }

    /// <summary>
    /// Tries to parse a single trivia token.
    /// </summary>
    /// <param name="result">The parsed trivia token is written here.</param>
    /// <returns>True, if a trivia token was parsed.</returns>
    private bool TryParseTrivia([MaybeNullWhen(false)] out IToken result)
    {
        var ch = this.Peek();
        // Newline
        if (this.TryParseNewline(0, out var newlineLength))
        {
            result = IToken.From(TokenType.Newline, this.AdvanceWithText(newlineLength));
            return true;
        }
        // Any horizontal whitespace
        if (IsSpace(ch))
        {
            // We merge it into one chunk to not produce so many individual tokens
            var offset = 1;
            while (IsSpace(this.Peek(offset))) ++offset;
            result = IToken.From(TokenType.Whitespace, this.AdvanceWithText(offset));
            return true;
        }
        // Line-comment
        if (ch == '/' && this.Peek(1) == '/')
        {
            var offset = 2;
            // NOTE: We use a little trick here, we specify a newline character as the default for Peek,
            // which means that this will terminate, even if the comment was on the last line of the file
            // without a line break
            while (!IsNewline(this.Peek(offset, @default: '\n'))) ++offset;
            result = IToken.From(TokenType.LineComment, this.AdvanceWithText(offset));
            return true;
        }
        // Not trivia
        result = null;
        return false;
    }

    /// <summary>
    /// Attempts to parse a newline.
    /// </summary>
    /// <param name="offset">The offset to start parsing from.</param>
    /// <param name="length">The length of the newline in characters.</param>
    /// <returns>True, if a newline was parsed, false otherwise.</returns>
    private bool TryParseNewline(int offset, out int length)
    {
        var ch = this.SourceReader.Peek(offset);
        if (ch == '\r')
        {
            // OS-X 9 or Windows-style newline
            if (this.SourceReader.Peek(offset + 1) == '\n')
            {
                // Windows-style
                length = 2;
                return true;
            }
            // OS-X 9 style
            length = 1;
            return true;
        }
        if (ch == '\n')
        {
            // UNIX-style newline
            length = 1;
            return true;
        }
        // Not a newline
        length = 0;
        return false;
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
    private ReadOnlyMemory<char> Advance(int amount) => this.SourceReader.Advance(amount);
    private string AdvanceWithText(int amount) => this.Advance(amount).ToString();

    // Character categorization
    private static bool IsIdent(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    private static bool IsSpace(char ch) => char.IsWhiteSpace(ch) && !IsNewline(ch);
    private static bool IsNewline(char ch) => ch == '\r' || ch == '\n';
    private static bool IsHexDigit(char ch) =>
           (ch >= '0' && ch <= '9')
        || (ch >= 'a' && ch <= 'f')
        || (ch >= 'A' && ch <= 'F');
    private static bool TryParseHexDigit(char ch, out int value)
    {
        if (ch >= '0' && ch <= '9')
        {
            value = ch - '0';
            return true;
        }
        if (ch >= 'a' && ch <= 'z')
        {
            value = ch - 'a' + 10;
            return true;
        }
        if (ch >= 'A' && ch <= 'Z')
        {
            value = ch - 'A' + 10;
            return true;
        }
        value = 0;
        return false;
    }
}
