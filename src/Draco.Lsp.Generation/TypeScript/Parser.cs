using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Draco.Lsp.Generation.TypeScript;

/// <summary>
/// Parses TypeScript code.
/// </summary>
internal sealed class Parser
{
    /// <summary>
    /// Parses TypeScript code into a TypeScript model.
    /// </summary>
    /// <param name="tokens">The sequence of tokens to parse.</param>
    /// <returns>The parsed TypeScript model.</returns>
    public static Model Parse(IEnumerable<Token> tokens) =>
        new Parser(tokens).ParseModel();

    private readonly IEnumerator<Token> tokens;
    private readonly List<Token> peekBuffer = new();

    private Parser(IEnumerable<Token> tokens)
    {
        this.tokens = tokens.GetEnumerator();
    }

    private Model ParseModel()
    {
        var elements = ImmutableArray.CreateBuilder<Declaration>();
        while (!this.Matches(TokenKind.EndOfInput)) elements.Add(this.ParseModelElement());
        return new(elements.ToImmutable());
    }

    private Declaration ParseModelElement()
    {
        // Eat qualifiers we don't care about
        this.Matches(TokenKind.KeywordExport);

        var peek = this.Peek();
        switch (peek.Type)
        {
        case TokenKind.KeywordInterface:
            return this.ParseInterface();

        case TokenKind.KeywordType:
            return this.ParseTypeAlias();

        default:
            throw new InvalidOperationException($"unexpected token {peek}");
        }
    }

    private TypeAlias ParseTypeAlias()
    {
        var doc = this.Expect(TokenKind.KeywordType).LeadingComment;
        var name = this.Expect(TokenKind.Name).Text;
        this.Expect(TokenKind.Assign);
        var type = this.ParseType();
        this.Expect(TokenKind.Semicolon);
        return new(doc, name, type);
    }

    private Interface ParseInterface()
    {
        var doc = this.Expect(TokenKind.KeywordInterface).LeadingComment;

        var name = this.Expect(TokenKind.Name).Text;
        var fields = ImmutableArray.CreateBuilder<Field>();

        this.Expect(TokenKind.CurlyOpen);
        while (!this.Matches(TokenKind.CurlyClose))
        {
            var field = this.ParseField();
            fields.Add(field);
        }

        return new(doc, name, fields.ToImmutable());
    }

    private Field ParseField()
    {
        var peek = this.Peek();
        switch (peek.Type)
        {
        case TokenKind.Name:
            return this.ParseSimpleField();
        case TokenKind.BracketOpen:
            return this.ParseIndexSignature();
        default:
            throw new InvalidOperationException($"unexpected token {peek}");
        }
    }

    private SimpleField ParseSimpleField()
    {
        var nameToken = this.Expect(TokenKind.Name);

        var doc = nameToken.LeadingComment;
        var name = nameToken.Text;

        var nullable = this.Matches(TokenKind.QuestionMark);

        this.Expect(TokenKind.Colon);

        var type = this.ParseType();
        // Optionally eat semicolon
        this.Matches(TokenKind.Semicolon);

        return new(doc, name, nullable, type);
    }

    private IndexSignature ParseIndexSignature()
    {
        this.Expect(TokenKind.BracketOpen);

        var keyName = this.Expect(TokenKind.Name).Text;

        this.Expect(TokenKind.Colon);
        var keyType = this.ParseType();

        this.Expect(TokenKind.BracketClose);

        this.Expect(TokenKind.Colon);
        var valueType = this.ParseType();

        // Optionally eat semicolon
        this.Matches(TokenKind.Semicolon);

        return new(keyName, keyType, valueType);
    }

    private Type ParseType()
    {
        var elements = ImmutableArray.CreateBuilder<Type>();
        elements.Add(this.ParsePostfixType());
        while (this.Matches(TokenKind.Pipe)) elements.Add(this.ParsePostfixType());
        return elements.Count == 1
            ? elements[0]
            : new UnionType(elements.ToImmutable());
    }

    private Type ParsePostfixType()
    {
        var result = this.ParseAtomicType();
        while (this.Matches(TokenKind.BracketOpen))
        {
            // Array
            this.Expect(TokenKind.BracketClose);
            result = new ArrayType(result);
        }
        return result;
    }

    private Type ParseAtomicType()
    {
        var peek = this.Peek();
        if (peek.Type == TokenKind.Name)
        {
            var name = this.Expect(TokenKind.Name).Text;
            return new NameType(name);
        }
        else if (peek.Type == TokenKind.CurlyOpen)
        {
            // Anonymous type
            var fields = ImmutableArray.CreateBuilder<Field>();
            this.Expect(TokenKind.CurlyOpen);
            while (!this.Matches(TokenKind.CurlyClose))
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

    private Token Expect(TokenKind type)
    {
        if (!this.Matches(type, out var result))
        {
            throw new InvalidOperationException($"Expected token {type}, but got {this.Peek()}");
        }
        return result;
    }

    private bool Matches(TokenKind type) => this.Matches(type, out _);

    private bool Matches(TokenKind type, [MaybeNullWhen(false)] out Token token)
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
            if (!this.tokens.MoveNext()) return new(TokenKind.EndOfInput, string.Empty, string.Empty);
            this.peekBuffer.Add(this.tokens.Current);
        }
        return this.peekBuffer[offset];
    }
}
