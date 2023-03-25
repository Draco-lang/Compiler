using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// Lexes TypeScript code.
/// </summary>
internal sealed class Lexer
{
    /// <summary>
    /// Tokenizes the given TypeScript source.
    /// </summary>
    /// <param name="text">The source to tokenize.</param>
    /// <returns>The sequence of typescript tokens.</returns>
    public static IEnumerable<Token> Lex(string text) => Lex(new StringReader(text));

    /// <summary>
    /// Tokenizes the given TypeScript source.
    /// </summary>
    /// <param name="reader">The source to tokenize.</param>
    /// <returns>The sequence of typescript tokens.</returns>
    public static IEnumerable<Token> Lex(TextReader reader)
    {
        var lexer = new Lexer(reader);
        while (true)
        {
            var token = lexer.Next();
            yield return token;
            if (token.Kind == TokenKind.EndOfInput) break;
        }
    }

    private bool IsEnd => this.Peek() == '\0';

    private readonly TextReader reader;
    private List<char> peekBuffer = new();
    private string? lastComment;

    private Lexer(TextReader reader)
    {
        this.reader = reader;
    }

    private Token Next()
    {
    begin:
        if (this.IsEnd) return this.MakeToken(TokenKind.EndOfInput, string.Empty);
        var ch = this.Peek();

        if (char.IsWhiteSpace(ch))
        {
            this.Advance();
            goto begin;
        }

        switch (ch)
        {
        case '/':
        {
            if (this.Peek(1) == '*')
            {
                // Multi-line comment
                var offset = 2;
                while (this.Peek(offset, '*') != '*' || this.Peek(offset + 1, '/') != '/') ++offset;
                offset += 2;
                this.lastComment = this.TakeString(offset);
                goto begin;
            }
            if (this.Peek(1) == '/')
            {
                // Single-line comment
                var offset = 2;
                while (!IsNewline(this.Peek(offset, '\n'))) ++offset;
                this.lastComment = this.TakeString(offset);
                goto begin;
            }
            break;
        }
        case '.': return this.Take(TokenKind.Dot, 1);
        case ',': return this.Take(TokenKind.Comma, 1);
        case ':': return this.Take(TokenKind.Colon, 1);
        case ';': return this.Take(TokenKind.Semicolon, 1);
        case '?': return this.Take(TokenKind.QuestionMark, 1);
        case '=': return this.Take(TokenKind.Assign, 1);
        case '|': return this.Take(TokenKind.Pipe, 1);
        case '-': return this.Take(TokenKind.Minus, 1);

        case '(': return this.Take(TokenKind.ParenOpen, 1);
        case ')': return this.Take(TokenKind.ParenClose, 1);
        case '{': return this.Take(TokenKind.CurlyOpen, 1);
        case '}': return this.Take(TokenKind.CurlyClose, 1);
        case '[': return this.Take(TokenKind.BracketOpen, 1);
        case ']': return this.Take(TokenKind.BracketClose, 1);
        case '<': return this.Take(TokenKind.LessThan, 1);
        case '>': return this.Take(TokenKind.GreaterThan, 1);
        }

        if (ch is '\'' or '"')
        {
            var closeQuotes = ch;
            var offset = 1;
            while (this.Peek(offset, closeQuotes) != closeQuotes)
            {
                if (this.Peek(offset) == '\\') offset += 2;
                else ++offset;
            }
            ++offset;
            return this.Take(TokenKind.LiteralString, offset);
        }

        if (char.IsDigit(ch))
        {
            var offset = 1;
            while (char.IsDigit(this.Peek(offset))) ++offset;
            return this.Take(TokenKind.LiteralInt, offset);
        }

        if (IsIdent(ch))
        {
            var offset = 1;
            while (IsIdent(this.Peek(offset))) ++offset;
            var text = this.TakeString(offset);
            var kind = text switch
            {
                "const" => TokenKind.KeywordConst,
                "enum" => TokenKind.KeywordEnum,
                "export" => TokenKind.KeywordExport,
                "extends" => TokenKind.KeywordExtends,
                "interface" => TokenKind.KeywordInterface,
                "namespace" => TokenKind.KeywordNamespace,
                "readonly" => TokenKind.KeywordReadonly,
                "type" => TokenKind.KeywordType,
                _ => TokenKind.Name,
            };
            return this.MakeToken(kind, text);
        }

        throw new InvalidOperationException($"unknown character {this.Peek()}");
    }

    private Token MakeToken(TokenKind type, string text)
    {
        var comment = this.lastComment;
        this.lastComment = null;
        return new(type, text, comment);
    }

    private Token Take(TokenKind type, int length)
    {
        var text = this.PeekString(length);
        this.Advance(length);
        return this.MakeToken(type, text);
    }

    private string TakeString(int length)
    {
        var result = this.PeekString(length);
        this.Advance(length);
        return result;
    }

    private string PeekString(int length)
    {
        if (length == 0) return string.Empty;
        this.Peek(length - 1);
        return new string(this.peekBuffer.Take(length).ToArray());
    }

    private void Advance(int length = 1)
    {
        if (length == 0) return;
        this.Peek(length - 1);
        this.peekBuffer.RemoveRange(0, length);
    }

    private char Peek(int offset = 0, char @default = '\0')
    {
        while (this.peekBuffer.Count <= offset)
        {
            var ch = this.reader.Read();
            if (ch == -1) return @default;
            this.peekBuffer.Add((char)ch);
        }
        return this.peekBuffer[offset];
    }

    private static bool IsIdent(char ch) => char.IsLetterOrDigit(ch) || ch == '_';
    private static bool IsNewline(char ch) => ch is '\r' or '\n';
}
