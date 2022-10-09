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
        if (this.SourceReader.IsEnd) return new(TokenType.EndOfInput, string.Empty);

        var ch = this.Peek();

        // NOTE: We are using the this.Skip method at quite a few places
        // This means we have quite a bit of redundancy, while we could just use this.Take, because the length
        // would be harder to mess up. I have two reasons to use this.Skip everywhere possible:
        //  - this.Take builds up a new string, while this.Skip uses string constants. My hope is that this
        //    results in less allocations overall.
        //  - This is a good indicator for which tokens don't need text stored, as it could be inferred from the
        //    token type. The only exception would be the keywords, we construct them from identifier tokens currently.

        // Newlines
        if (ch == '\r')
        {
            // Windows-style newline
            if (this.Peek(1) == '\n') return this.Skip(TokenType.Newline, "\r\n");
            // OS-X 9-style newline
            return this.Skip(TokenType.Newline, "\r");
        }
        if (ch == '\n')
        {
            // UNIX-style newline
            return this.Skip(TokenType.Newline, "\n");
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
        case '(': return this.Skip(TokenType.ParenOpen, "(");
        case ')': return this.Skip(TokenType.ParenClose, ")");
        case '{': return this.Skip(TokenType.CurlyOpen, "{");
        case '}': return this.Skip(TokenType.CurlyClose, "}");
        case '[': return this.Skip(TokenType.BracketOpen, "[");
        case ']': return this.Skip(TokenType.BracketClose, "]");

        case '.': return this.Skip(TokenType.Dot, ".");
        case ',': return this.Skip(TokenType.Comma, ",");
        case ':': return this.Skip(TokenType.Colon, ":");
        case ';': return this.Skip(TokenType.Semicolon, ";");
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
            var newTokenType = token.Text switch
            {
                "from" => TokenType.KeywordFrom,
                "func" => TokenType.KeywordFunc,
                "import" => TokenType.KeywordImport,
                _ => TokenType.Identifier,
            };
            return new(newTokenType, token.Text);
        }

        // Unknown
        return this.Take(TokenType.Unknown, 1);
    }

    // Utilities for token construction
    private Token Take(TokenType tokenType, int length)
    {
        var sb = new StringBuilder();
        for (var i = 0; i < length; ++i) sb.Append(this.Peek(i));
        return this.Skip(tokenType, sb.ToString());
    }

    private Token Skip(TokenType tokenType, string text)
    {
        this.Advance(text.Length);
        return new(tokenType, text);
    }

    // Propagating functions to the source reader to decouple the API a bit, in case it changes
    // later for performance reasons
    private char Peek(int offset = 0, char @default = '\0') =>
        this.SourceReader.Peek(offset: offset, @default: @default);
    private void Advance(int amount = 1) => this.SourceReader.Advance(amount);

    // Character categorization
    private static bool IsIdent(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    private static bool IsSpace(char ch) => char.IsWhiteSpace(ch) && !IsNewline(ch);
    private static bool IsNewline(char ch) => ch == '\r' || ch == '\n';
}
