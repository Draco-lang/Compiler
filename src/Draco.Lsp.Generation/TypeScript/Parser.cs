using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Lsp.Generation.TypeScript;

internal sealed class Parser
{
    public static ModelElement Parse(IEnumerable<Token> tokens) =>
        new Parser(tokens).ParseModelElement();

    private readonly IEnumerator<Token> tokens;
    private readonly List<Token> peekBuffer = new();

    private Parser(IEnumerable<Token> tokens)
    {
        this.tokens = tokens.GetEnumerator();
    }

    private ModelElement ParseModelElement()
    {
        var peek = this.Peek();
        if (peek.Type == TokenType.KeywordInterface)
        {
            return this.ParseInterface();
        }
        else
        {
            throw new InvalidOperationException($"unexpected token {peek}");
        }
    }

    private InterfaceModel ParseInterface()
    {
        var doc = this.ParseOptionalDocumentation();

        this.Expect(TokenType.KeywordInterface);

        var name = this.Expect(TokenType.Name).Text;
        var fields = ImmutableArray.CreateBuilder<Field>();

        this.Expect(TokenType.CurlyOpen);
        while (!this.Matches(TokenType.CurlyClose))
        {
            var field = this.ParseField();
            fields.Add(field);
        }

        return new(doc, name, fields.ToImmutable());
    }

    private Field ParseField()
    {
        var doc = this.ParseOptionalDocumentation();

        var name = this.Expect(TokenType.Name).Text;
        var nullable = this.Matches(TokenType.QuestionMark);

        this.Expect(TokenType.Colon);

        var type = this.ParseType();
        // Optionally eat semicolon
        this.Matches(TokenType.Semicolon);

        return new(doc, name, nullable, type);
    }

    private ModelType ParseType()
    {
        return this.ParsePostfixType();
    }

    private ModelType ParsePostfixType()
    {
        var result = this.ParseAtomicType();
        while (this.Matches(TokenType.BracketOpen))
        {
            // Array
            this.Expect(TokenType.BracketClose);
            result = new ArrayType(result);
        }
        return result;
    }

    private ModelType ParseAtomicType()
    {
        var peek = this.Peek();
        if (peek.Type == TokenType.Name)
        {
            var name = this.Expect(TokenType.Name).Text;
            return new NameType(name);
        }
        else if (peek.Type == TokenType.CurlyOpen)
        {
            // Anonymous type
            var fields = ImmutableArray.CreateBuilder<Field>();
            this.Expect(TokenType.CurlyOpen);
            while (!this.Matches(TokenType.CurlyClose))
            {
                var field = this.ParseField();
                fields.Add(field);
            }
            return new AnonymousType(fields.ToImmutable());
        }
        else
        {
            throw new InvalidOperationException($"unexpected token {peek}");
        }
    }

    private string? ParseOptionalDocumentation()
    {
        if (this.Matches(TokenType.Comment, out var comment)) return comment.Text;
        return null;
    }

    private Token Expect(TokenType type)
    {
        if (!this.Matches(type, out var result))
        {
            throw new InvalidOperationException($"Expected token {type}, but got {this.Peek()}");
        }
        return result;
    }

    private bool Matches(TokenType type) => this.Matches(type, out _);

    private bool Matches(TokenType type, [MaybeNullWhen(false)] out Token token)
    {
        if (this.Peek().Type == type)
        {
            token = this.Advance();
            return true;
        }
        else
        {
            token = default;
            return false;
        }
    }

    private Token Advance()
    {
        this.Peek();
        var result = this.peekBuffer[0];
        this.peekBuffer.RemoveAt(0);
        return result;
    }

    private Token Peek(int offset = 0)
    {
        while (this.peekBuffer.Count <= offset)
        {
            if (!this.tokens.MoveNext()) return new(TokenType.EndOfInput, string.Empty);
            this.peekBuffer.Add(this.tokens.Current);
        }
        return this.peekBuffer[offset];
    }
}
