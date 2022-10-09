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
    /// The reader the source text is read from.
    /// </summary>
    public ISourceReader SourceReader { get; }

    public Lexer(ISourceReader sourceReader)
    {
        this.SourceReader = sourceReader;
    }

    /// <summary>
    /// Reads the next <see cref="Token"/> from the input.
    /// </summary>
    /// <returns>The <see cref="Token"/> read.</returns>
    public Token Next()
    {
        // End of input
        if (this.SourceReader.IsEnd) return new(TokenType.EndOfInput, ReadOnlyMemory<char>.Empty);

        var ch = this.Peek();

        // Newlines
        if (ch == '\r')
        {
            // Windows-style newline
            if (this.Peek(1) == '\n') return this.Take(TokenType.Newline, 2);
            // OS-X 9-style newline
            return this.Take(TokenType.Newline, 1);
        }
        if (ch == '\n')
        {
            // UNIX-style newline
            return this.Take(TokenType.Newline, 1);
        }

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
        case '{': return this.Take(TokenType.CurlyOpen, 1);
        case '}': return this.Take(TokenType.CurlyClose, 1);
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
                _ => TokenType.Identifier,
            };
            return new(newTokenType, token.Text);
        }

        // Unknown
        return this.Take(TokenType.Unknown, 1);
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
}
