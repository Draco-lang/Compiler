using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.ParseTree;
using DiagnosticTemplate = Draco.Compiler.Api.Diagnostics.DiagnosticTemplate;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Breaks up source code into a sequence of <see cref="Token"/>s.
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
        /// Normal source code within line string interpolation.
        /// </summary>
        LineInterpolation,

        /// <summary>
        /// Normal source code within multi-line string interpolation.
        /// </summary>
        MultiLineInterpolation,

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
    internal readonly record struct Mode(ModeKind Kind, int ExtendedDelims)
    {
        /// <summary>
        /// True, if the mode is some kind of interpolation mode.
        /// </summary>
        public bool IsInterpolation => this.Kind
            is ModeKind.LineInterpolation
            or ModeKind.MultiLineInterpolation;

        /// <summary>
        /// True, if this mode is some kind of string lexing mode.
        /// </summary>
        public bool IsString => this.Kind
            is ModeKind.LineString
            or ModeKind.MultiLineString;

        /// <summary>
        /// True, if this is some code lexing mode, either regular or interpolation.
        /// </summary>
        public bool IsCode => this.Kind == ModeKind.Normal || this.IsInterpolation;

        /// <summary>
        /// True, if the current mode must not hold a newline.
        /// </summary>
        public bool IsLine => this.Kind
            is ModeKind.LineString
            or ModeKind.LineInterpolation;
    }

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
    private readonly Token.Builder tokenBuilder = new();
    private readonly StringBuilder valueBuilder = new();
    private readonly ImmutableArray<Trivia>.Builder leadingTriviaList = ImmutableArray.CreateBuilder<Trivia>();
    private readonly ImmutableArray<Trivia>.Builder trailingTriviaList = ImmutableArray.CreateBuilder<Trivia>();
    private readonly ImmutableArray<Diagnostic>.Builder diagnosticList = ImmutableArray.CreateBuilder<Diagnostic>();

    public Lexer(ISourceReader sourceReader)
    {
        this.SourceReader = sourceReader;
        this.PushMode(ModeKind.Normal, 0);
    }

    /// <summary>
    /// Reads the next <see cref="Token"/> from the input.
    /// </summary>
    /// <returns>The <see cref="Token"/> read.</returns>
    public Token Lex()
    {
        this.tokenBuilder.Clear();
        this.valueBuilder.Clear();
        this.leadingTriviaList.Clear();
        this.trailingTriviaList.Clear();
        this.diagnosticList.Clear();

        switch (this.CurrentMode.Kind)
        {
        case ModeKind.Normal:
        case ModeKind.LineInterpolation:
        case ModeKind.MultiLineInterpolation:
        {
            // Normal tokens can have trivia
            this.ParseLeadingTriviaList();
            this.LexNormal();
            // If we just ended interpolation, we are within a string, don't consume
            if (this.tokenBuilder.Type != TokenType.InterpolationEnd) this.ParseTrailingTriviaList();
            break;
        }

        case ModeKind.LineString:
        case ModeKind.MultiLineString:
            this.LexString();
            // If we are starting interpolation, we can consume trailing trivia
            if (this.tokenBuilder.Type == TokenType.InterpolationStart) this.ParseTrailingTriviaList();
            break;

        default:
            throw new InvalidOperationException("unsupported lexer mode");
        }

        // If there was any leading or trailing trivia, we have to add them
        if (this.leadingTriviaList.Count > 0) this.tokenBuilder.SetLeadingTrivia(this.leadingTriviaList.ToImmutable());
        if (this.trailingTriviaList.Count > 0) this.tokenBuilder.SetTrailingTrivia(this.trailingTriviaList.ToImmutable());

        // Attach diagnostics, if any
        if (this.diagnosticList.Count > 0) this.tokenBuilder.SetDiagnostics(this.diagnosticList.ToImmutable());

        return this.tokenBuilder.Build();
    }

    /// <summary>
    /// Lexes tokens that can be found in regular code.
    /// </summary>
    /// <returns>The lexed <see cref="Token"/>.</returns>
    private Unit LexNormal()
    {
        Unit TakeBasic(TokenType tokenType, int length)
        {
            this.Advance(length);
            this.tokenBuilder.SetType(tokenType);
            return default;
        }

        Unit TakeWithText(TokenType tokenType, int length)
        {
            this.tokenBuilder
                .SetType(tokenType)
                .SetText(this.AdvanceWithText(length));
            return default;
        }

        // First check for end of source here
        if (this.SourceReader.IsEnd)
        {
            this.tokenBuilder.SetType(TokenType.EndOfInput);
            return default;
        }

        var mode = this.CurrentMode;
        var ch = this.Peek();

        // Punctuation
        switch (ch)
        {
        case '(': return TakeBasic(TokenType.ParenOpen, 1);
        case ')': return TakeBasic(TokenType.ParenClose, 1);
        case '{':
            if (mode.IsInterpolation) this.PushMode(mode.Kind, 0);
            return TakeBasic(TokenType.CurlyOpen, 1);
        case '}':
            if (mode.IsInterpolation)
            {
                this.PopMode();
                // If we are not in interpolation anymore, this is an interpolation end token
                if (!this.CurrentMode.IsInterpolation) return TakeBasic(TokenType.InterpolationEnd, 1);
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

            // Floating point number
            if (this.Peek(offset) == '.' && char.IsDigit(this.Peek(offset + 1)))
            {
                offset += 2;
                while (char.IsDigit(this.Peek(offset))) ++offset;
                var floatView = this.Advance(offset);
                // TODO: Parsing into an float32 might not be the best idea
                var floatValue = float.Parse(floatView.Span, provider: CultureInfo.InvariantCulture);
                this.tokenBuilder
                    .SetType(TokenType.LiteralFloat)
                    .SetText(floatView.ToString())
                    .SetValue(floatValue);
                return default;
            }

            // Regular integer
            var intView = this.Advance(offset);
            // TODO: Parsing into an int32 might not be the best idea
            var intValue = int.Parse(intView.Span);
            this.tokenBuilder
                .SetType(TokenType.LiteralInteger)
                .SetText(intView.ToString())
                .SetValue(intValue);
            return default;
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
                // NOTE: We don't necessarily need to save the value yet, but if we later decide that we want
                // escaped identifiers, this can be a good way to handle it
                var identStr = ident.ToString();
                this.tokenBuilder
                    .SetType(TokenType.Identifier)
                    .SetText(identStr)
                    .SetValue(identStr);
                return default;
            }
            else
            {
                // Keyword, we save on allocation
                this.tokenBuilder.SetType(tokenType);
                return default;
            }
        }

        // Character literals
        if (ch == '\'')
        {
            var offset = 1;
            var ch2 = this.Peek(offset);
            var resultChar = string.Empty;
            if (ch2 == '\\')
            {
                // Escape-sequence
                ++offset;
                resultChar = this.ParseEscapeSequence(offset - 1, ref offset);
            }
            else if (!char.IsControl(ch2))
            {
                // Regular character
                ++offset;
                resultChar = ch2.ToString();
            }
            else
            {
                // Error, illegal character
                this.AddError(
                    SyntaxErrors.IllegalCharacterLiteral,
                    offset: offset,
                    width: 1,
                    args: (int)ch2);
                resultChar = " ";
            }
            // Expect closing quote
            if (this.Peek(offset) == '\'')
            {
                ++offset;
            }
            else
            {
                // NOTE: We could have some strategy to try to look for closing quotes to try to sync the lexer
                // Maybe we could search for the next quotes as long as we are in-line?
                this.AddError(
                    SyntaxErrors.UnclosedCharacterLiteral,
                    offset: offset,
                    width: 1);
            }
            // Done
            var text = this.AdvanceWithText(offset);
            this.tokenBuilder
                .SetType(TokenType.LiteralCharacter)
                .SetText(text)
                .SetValue(resultChar);
            return default;
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

    /// <summary>
    /// Lexes a token that can be part of a string.
    /// </summary>
    /// <returns>The lexed string <see cref="Token"/>.</returns>
    private Unit LexString()
    {
        // Get the largest continuous sequence without linebreaks or interpolation
        var mode = this.CurrentMode;
        var offset = 0;

    start:
        // First check for end of source here
        if (this.SourceReader.IsEnd)
        {
            this.tokenBuilder.SetType(TokenType.EndOfInput);
            return default;
        }

        var ch = this.Peek(offset);

        // NOTE: We are checking end of input differently here, because SourceReader.IsEnd is based on its
        // current position, but we are peeking in this input way ahead
        // End of input
        if (ch == '\0' && offset > 0)
        {
            // NOTE: Not a nice assumption to rely on '\0', but let's assume the input could end here,
            // return the section we have consumed so far
            this.tokenBuilder
                .SetType(TokenType.StringContent)
                .SetText(this.AdvanceWithText(offset))
                .SetValue(this.valueBuilder.ToString());
            return default;
        }

        // Check for closing quotes
        // For multiline strings we allow horizontal whitespace
        // This only happens for empty strings, but we still need to check if
        var stringEndOffset = offset;
        var stringEndCh = ch;
        if (mode.Kind == ModeKind.MultiLineString)
        {
            // Consume leading space
            while (IsSpace(this.Peek(stringEndOffset))) ++stringEndOffset;
            stringEndCh = this.Peek(stringEndOffset);
        }
        if (stringEndCh == '"')
        {
            var endLength = 0;
            if (mode.Kind == ModeKind.MultiLineString)
            {
                // We are expecting 2 more quotes
                if (this.Peek(stringEndOffset + 1) != '"' || this.Peek(stringEndOffset + 2) != '"') goto not_string_end;
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
                if (this.Peek(stringEndOffset + endLength + i) != '#') goto not_string_end;
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
                // In case the string end offset doesn't match, we have leading whitespace
                if (offset != stringEndOffset) this.ParseLeadingTriviaList();
                var tokenType = mode.Kind == ModeKind.LineString
                    ? TokenType.LineStringEnd
                    : TokenType.MultiLineStringEnd;
                this.tokenBuilder
                    .SetType(tokenType)
                    .SetText(this.AdvanceWithText(endLength));
                return default;
            }
            else
            {
                // This will only be the end of string in the next iteration, we just return what we have
                // consumed so far
                this.tokenBuilder
                    .SetType(TokenType.StringContent)
                    .SetText(this.AdvanceWithText(offset))
                    .SetValue(this.valueBuilder.ToString());
                return default;
            }
        }

    not_string_end:
        // Check for escape sequence
        if (ch == '\\')
        {
            var escapeStart = offset;

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
                    var newMode = mode.Kind == ModeKind.LineString
                        ? ModeKind.LineInterpolation
                        : ModeKind.MultiLineInterpolation;
                    this.PushMode(newMode, 0);
                    this.tokenBuilder
                        .SetType(TokenType.InterpolationStart)
                        .SetText(this.AdvanceWithText(mode.ExtendedDelims + 2));
                    return default;
                }
                else
                {
                    // This will only be interpolation in the next iteration, we just return what we have
                    // consumed so far
                    this.tokenBuilder
                        .SetType(TokenType.StringContent)
                        .SetText(this.AdvanceWithText(offset))
                        .SetValue(this.valueBuilder.ToString());
                    return default;
                }
            }

            // Line continuation
            if (mode.Kind == ModeKind.MultiLineString)
            {
                var offset2 = offset + mode.ExtendedDelims + 1;
                var whiteCharOffset = 0;
                while (IsSpace(this.Peek(offset2 + whiteCharOffset))) whiteCharOffset++;
                if (this.TryParseNewline(offset2 + whiteCharOffset, out var length))
                {
                    //TODO: decide what to do if there are whitespaces after line continuation
                    if (offset == 0)
                    {
                        // We can return this line-continuation
                        // The important thing is that the value is an empty string
                        this.tokenBuilder
                            .SetType(TokenType.StringNewline)
                            .SetText(this.AdvanceWithText(mode.ExtendedDelims + 1 + whiteCharOffset + length))
                            .SetValue(string.Empty);
                        return default;
                    }
                    else
                    {
                        // There is content before that we need to deal with
                        this.tokenBuilder
                            .SetType(TokenType.StringContent)
                            .SetText(this.AdvanceWithText(offset))
                            .SetValue(this.valueBuilder.ToString());
                        return default;
                    }
                }
            }

            offset += mode.ExtendedDelims + 1;
            // Try to parse an escape
            var escaped = this.ParseEscapeSequence(escapeStart, ref offset);
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
                // A newline is illegal in a line string literal
                // NOTE: While we end the string mode, we DO NOT REPORT THE ERROR HERE
                // To be consistent about responsibility, matching up token pairs will be the job of the parser
                // It would be really messy to be able to report this same error for multiline strings
                // Also, this is only triggered if a newline is met, but not on the end of file
                this.PopMode();
                while (this.CurrentMode.IsLine) this.PopMode();
                this.tokenBuilder
                    .SetType(TokenType.StringContent)
                    .SetText(this.AdvanceWithText(offset))
                    .SetValue(this.valueBuilder.ToString());
                return default;
            }
            else
            {
                // Newlines are completely valid in multiline strings
                if (offset == 0)
                {
                    // We are fine to return this newline, if it is indeed a newline
                    // If after this newline it's only whitespace and then the end quotes, we simply don't
                    // need to include this as a string newline
                    var whiteOffset = 0;
                    while (IsSpace(this.Peek(newlineLength + whiteOffset))) ++whiteOffset;
                    var endOffset = newlineLength + whiteOffset;
                    if (this.Peek(endOffset) == '"' && this.Peek(endOffset + 1) == '"' && this.Peek(endOffset + 2) == '"')
                    {
                        // The last check we need to do is to make sure we have enough delimiters
                        for (var i = 0; i < mode.ExtendedDelims; ++i)
                        {
                            if (this.Peek(endOffset + 3 + i) != '#') goto not_string_end2;
                        }
                        // This is the last newline and we have no other content to consume
                        // We build the end token with trivia
                        this.PopMode();
                        this.ParseLeadingTriviaList();
                        Debug.Assert(this.leadingTriviaList.Count is 1 or 2);
                        this.tokenBuilder
                            .SetType(TokenType.MultiLineStringEnd)
                            .SetText(this.AdvanceWithText(3 + mode.ExtendedDelims));
                        this.ParseTrailingTriviaList();
                        return default;
                    }
                not_string_end2:
                    // Just a regular newline, more content to follow
                    var stringNewlineText = this.AdvanceWithText(newlineLength);
                    this.tokenBuilder
                        .SetType(TokenType.StringNewline)
                        .SetText(stringNewlineText)
                        .SetValue(stringNewlineText);
                    return default;
                }
                else
                {
                    // We need to return the content fist
                    this.tokenBuilder
                        .SetType(TokenType.StringContent)
                        .SetText(this.AdvanceWithText(offset))
                        .SetValue(this.valueBuilder.ToString());
                    return default;
                }
            }
        }

        // Just consume as a content character
        this.valueBuilder.Append(ch);
        ++offset;
        goto start;
    }

    // NOTE: We are returning a string here because UTF32 > UTF16...
    // We might want to switch to something more efficient later for performance reasons
    /// <summary>
    /// Parses an escape sequence for strings and character literals.
    /// Reports an error, if the escape sequence is illegal.
    /// </summary>
    /// <param name="escapeStart">The position where the escape character occurred.</param>
    /// <param name="offset">A reference to an offset that points after the backslash and optional extended
    /// delimiter characters. If parsing the escape succeeded, the result is written back here.</param>
    /// <returns>True, if an escape was successfully parsed.</returns>
    private string ParseEscapeSequence(int escapeStart, ref int offset)
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
                    return char.ConvertFromUtf32(unicodeValue);
                }
                else
                {
                    this.AddError(
                        SyntaxErrors.ZeroLengthUnicodeCodepoint,
                        offset: escapeStart,
                        width: offset - escapeStart);
                    // We just return an empty character
                    return string.Empty;
                }
            }
            else
            {
                this.AddError(
                    SyntaxErrors.UnclosedUnicodeCodepoint,
                    offset: offset,
                    width: 1);
                // We just return an empty character
                return string.Empty;
            }
        }
        // Any single-character escape, find the escaped equivalent
        var escaped = esc switch
        {
            '0' => "\0",
            'a' => "\a",
            'b' => "\b",
            'f' => "\f",
            'n' => "\n",
            'r' => "\r",
            't' => "\t",
            'v' => "\v",
            '\'' => "\'",
            '\"' => "\"",
            '\\' => "\\",
            _ => null,
        };
        if (escaped is not null)
        {
            ++offset;
            return escaped;
        }
        else
        {
            this.AddError(
                SyntaxErrors.IllegalEscapeCharacter,
                offset: escapeStart,
                width: 2,
                args: esc);
            ++offset;
            // We return the escaped character literally as a substitute
            return esc.ToString();
        }
    }

    /// <summary>
    /// Parses leading trivia into <see cref="leadingTriviaList"/>. Leading trivia is essentially all trivia
    /// tokens until the next significant token.
    /// </summary>
    private void ParseLeadingTriviaList()
    {
        while (this.TryParseTrivia(out var trivia)) this.leadingTriviaList.Add(trivia);
    }

    /// <summary>
    /// Parses trailing trivia into <see cref="trailingTriviaList"/>. Trailing trivia is essentially all trivia
    /// tokens until the next end of line, which is also included in the list.
    /// </summary>
    private void ParseTrailingTriviaList()
    {
        while (this.TryParseTrivia(out var trivia))
        {
            this.trailingTriviaList.Add(trivia);
            if (trivia.Type == TriviaType.Newline) break;
        }
    }

    /// <summary>
    /// Tries to parse a single trivia token.
    /// </summary>
    /// <param name="result">The parsed trivia token is written here.</param>
    /// <returns>True, if a trivia token was parsed.</returns>
    private bool TryParseTrivia([MaybeNullWhen(false)] out Trivia result)
    {
        var ch = this.Peek();
        // Newline
        if (this.TryParseNewline(0, out var newlineLength))
        {
            if (this.CurrentMode.Kind == ModeKind.LineInterpolation)
            {
                // Illegal newline, we have to stop any nested interpolation and string lexing
                this.modeStack.Pop();
                while (this.CurrentMode.Kind != ModeKind.Normal) this.modeStack.Pop();
            }
            result = Trivia.From(TriviaType.Newline, this.AdvanceWithText(newlineLength));
            return true;
        }
        // Any horizontal whitespace
        if (IsSpace(ch))
        {
            // We merge it into one chunk to not produce so many individual tokens
            var offset = 1;
            while (IsSpace(this.Peek(offset))) ++offset;
            result = Trivia.From(TriviaType.Whitespace, this.AdvanceWithText(offset));
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
            result = Trivia.From(TriviaType.LineComment, this.AdvanceWithText(offset));
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

    // Errors
    private void AddError(DiagnosticTemplate template, int offset, int width, params object?[] args)
    {
        var range = new RelativeRange(Offset: offset, Width: width);
        var location = new Location.OnTree(Range: range);
        var diag = Diagnostic.Create(
            template: template,
            location: location,
            formatArgs: args);
        this.diagnosticList.Add(diag);
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
    private static bool TryParseHexDigit(char ch, out int value)
    {
        if (ch >= '0' && ch <= '9')
        {
            value = ch - '0';
            return true;
        }
        if (ch >= 'a' && ch <= 'f')
        {
            value = ch - 'a' + 10;
            return true;
        }
        if (ch >= 'A' && ch <= 'F')
        {
            value = ch - 'A' + 10;
            return true;
        }
        value = 0;
        return false;
    }
}
