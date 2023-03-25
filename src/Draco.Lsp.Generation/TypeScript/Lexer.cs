using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

internal sealed class Lexer
{
    public static IEnumerable<Token> Lex(TextReader reader)
    {
        var lexer = new Lexer(reader);
        while (true)
        {
            var token = lexer.Next();
            yield return token;
            if (token.Type == TokenType.EndOfInput) break;
        }
    }

    private bool IsEnd => this.Peek() == '\0';

    private readonly TextReader reader;
    private List<char> peekBuffer = new();

    private Lexer(TextReader reader)
    {
        this.reader = reader;
    }

    private Token Next()
    {
    begin:
        if (this.IsEnd) return new(TokenType.EndOfInput, string.Empty);
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
                return this.Take(TokenType.Comment, offset);
            }
            if (this.Peek(1) == '/')
            {
                // Single-line comment
                var offset = 2;
                while (!IsNewline(this.Peek(offset, '\n'))) ++offset;
                return this.Take(TokenType.Comment, offset);
            }
            break;
        }
        case ',': return this.Take(TokenType.Comma, 1);
        case ':': return this.Take(TokenType.Colon, 1);
        case ';': return this.Take(TokenType.Semicolon, 1);
        case '?': return this.Take(TokenType.QuestionMark, 1);

        case '(': return this.Take(TokenType.ParenOpen, 1);
        case ')': return this.Take(TokenType.ParenClose, 1);
        case '{': return this.Take(TokenType.CurlyOpen, 1);
        case '}': return this.Take(TokenType.CurlyClose, 1);
        case '[': return this.Take(TokenType.BracketOpen, 1);
        case ']': return this.Take(TokenType.BracketClose, 1);
        }

        if (IsIdent(ch))
        {
            var offset = 1;
            while (IsIdent(this.Peek(offset))) ++offset;
            var text = this.Take(offset);
            var kind = text switch
            {
                "interface" => TokenType.KeywordInterface,
                _ => TokenType.Name,
            };
            return new(kind, text);
        }

        throw new InvalidOperationException($"unknown character {this.Peek()}");
    }

    private Token Take(TokenType type, int length) =>
        new(type, this.Take(length));

    private string Take(int length)
    {
        if (length == 0) return string.Empty;
        this.Peek(length - 1);
        var result = new string(this.peekBuffer.Take(length).ToArray());
        this.peekBuffer.RemoveRange(0, length);
        return result;
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
