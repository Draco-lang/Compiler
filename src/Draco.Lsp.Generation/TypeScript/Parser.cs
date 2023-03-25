using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
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
    start:
        var peek = this.Peek();
        if (peek.Kind == TokenKind.CurlyOpen)
        {
            this.SkipStructure(TokenKind.CurlyOpen, TokenKind.CurlyClose);
            goto start;
        }
        if (peek.Kind == TokenKind.BracketOpen)
        {
            this.SkipStructure(TokenKind.BracketOpen, TokenKind.BracketClose);
            goto start;
        }
        if (peek.Kind
            is TokenKind.Name
            or TokenKind.Assign
            or TokenKind.Dot
            or TokenKind.Comma
            or TokenKind.Colon
            or TokenKind.Semicolon
            or TokenKind.QuestionMark)
        {
            this.Advance();
            goto start;
        }
        if (peek.Kind == TokenKind.KeywordExport) peek = this.Peek(1);
        return peek.Kind switch
        {
            TokenKind.KeywordConst => this.ParseConstant(),
            TokenKind.KeywordEnum => this.ParseEnum(),
            TokenKind.KeywordInterface => this.ParseInterface(),
            TokenKind.KeywordNamespace => this.ParseNamespace(),
            TokenKind.KeywordType => this.ParseTypeAlias(),
            _ => throw new InvalidOperationException($"unexpected token {peek}"),
        };
    }

    private void SkipStructure(TokenKind open, TokenKind close)
    {
        var depth = 0;
        while (true)
        {
            var t = this.Advance();
            if (t.Kind == open)
            {
                ++depth;
            }
            else if (t.Kind == close)
            {
                --depth;
                if (depth == 0) break;
            }
        }
    }

    private Enum ParseEnum()
    {
        var doc = this.ParseDocumentedPreamble(TokenKind.KeywordEnum, TokenKind.KeywordExport);

        var name = this.Expect(TokenKind.Name).Text;
        var members = ImmutableArray.CreateBuilder<KeyValuePair<string, Expression>>();

        this.Expect(TokenKind.CurlyOpen);
        while (!this.Matches(TokenKind.CurlyClose))
        {
            var memberName = this.ParseName();
            this.Expect(TokenKind.Assign);
            var memberValue = this.ParseExpression();
            members.Add(new(memberName, memberValue));

            // Optionally match comma
            this.Matches(TokenKind.Comma);
        }

        return new(doc, name, members.ToImmutable());
    }

    private Namespace ParseNamespace()
    {
        var doc = this.ParseDocumentedPreamble(TokenKind.KeywordNamespace, TokenKind.KeywordExport);

        var name = this.Expect(TokenKind.Name).Text;
        var constants = ImmutableArray.CreateBuilder<Constant>();

        this.Expect(TokenKind.CurlyOpen);
        while (!this.Matches(TokenKind.CurlyClose)) constants.Add(this.ParseConstant());

        return new(doc, name, constants.ToImmutable());
    }

    private Constant ParseConstant()
    {
        var doc = this.ParseDocumentedPreamble(TokenKind.KeywordConst, TokenKind.KeywordExport);

        var name = this.Expect(TokenKind.Name).Text;
        if (this.Matches(TokenKind.Colon))
        {
            // Skip type-annotation
            while (this.Peek().Kind != TokenKind.Assign) this.Advance();
        }

        this.Expect(TokenKind.Assign);

        var value = this.ParseExpression();
        this.Matches(TokenKind.Semicolon);

        return new(doc, name, value);
    }

    private Expression ParseExpression() => this.ParseBinaryExpression();

    private Expression ParseBinaryExpression()
    {
        var elements = ImmutableArray.CreateBuilder<Expression>();
        elements.Add(this.ParsePrefixExpression());
        while (this.Matches(TokenKind.Pipe)) elements.Add(this.ParsePrefixExpression());
        return elements.Count == 1
            ? elements[0]
            : new UnionTypeExpression(elements.ToImmutable());
    }

    private Expression ParsePrefixExpression()
    {
        if (this.Matches(TokenKind.Minus))
        {
            var op = this.ParsePostfixExpression();
            return new NegateExpression(op);
        }
        return this.ParsePostfixExpression();
    }

    private Expression ParsePostfixExpression()
    {
        var result = this.ParseMemberAccessExpression();
        while (this.Matches(TokenKind.BracketOpen))
        {
            this.Expect(TokenKind.BracketClose);
            result = new ArrayTypeExpression(result);
        }
        return result;
    }

    private Expression ParseMemberAccessExpression()
    {
        var result = this.ParseAtomExpression();
        while (this.Matches(TokenKind.Dot)) result = new MemberExpression(result, this.Expect(TokenKind.Name).Text);
        return result;
    }

    private Expression ParseAtomExpression()
    {
        if (this.Matches(TokenKind.LiteralInt, out var intLit)) return new IntExpression(int.Parse(intLit.Text));
        if (this.Matches(TokenKind.LiteralString, out var strLit)) return new StringExpression(strLit.Text);
        if (this.Matches(TokenKind.Name, out var name)) return new NameExpression(name.Text);
        if (this.Matches(TokenKind.BracketOpen))
        {
            var elements = ImmutableArray.CreateBuilder<Expression>();
            while (this.Peek().Kind != TokenKind.BracketClose)
            {
                elements.Add(this.ParseExpression());
                if (!this.Matches(TokenKind.Comma)) break;
            }
            this.Expect(TokenKind.BracketClose);
            return new ArrayExpression(elements.ToImmutable());
        }
        if (this.Matches(TokenKind.CurlyOpen))
        {
            // Anonymous type
            var fields = ImmutableArray.CreateBuilder<Field>();
            while (!this.Matches(TokenKind.CurlyClose))
            {
                var field = this.ParseField();
                fields.Add(field);
            }
            return new AnonymousTypeExpression(fields.ToImmutable());
        }
        if (this.Matches(TokenKind.ParenOpen))
        {
            var expr = this.ParseExpression();
            this.Expect(TokenKind.ParenClose);
            return expr;
        }

        throw new InvalidOperationException($"unexpected token {this.Peek()}");
    }

    private TypeAlias ParseTypeAlias()
    {
        var doc = this.ParseDocumentedPreamble(TokenKind.KeywordType, TokenKind.KeywordExport);
        var name = this.Expect(TokenKind.Name).Text;
        this.Expect(TokenKind.Assign);
        var type = this.ParseExpression();
        this.Expect(TokenKind.Semicolon);
        return new(doc, name, type);
    }

    private Interface ParseInterface()
    {
        var doc = this.ParseDocumentedPreamble(TokenKind.KeywordInterface, TokenKind.KeywordExport);

        var name = this.Expect(TokenKind.Name).Text;

        var genericParams = ImmutableArray.CreateBuilder<string>();
        if (this.Matches(TokenKind.LessThan))
        {
            genericParams.Add(this.Expect(TokenKind.Name).Text);
            while (this.Matches(TokenKind.Comma)) genericParams.Add(this.Expect(TokenKind.Name).Text);
            this.Expect(TokenKind.GreaterThan);
        }

        var bases = ImmutableArray.CreateBuilder<Expression>();
        if (this.Matches(TokenKind.KeywordExtends))
        {
            bases.Add(this.ParseExpression());
            while (this.Matches(TokenKind.Comma)) bases.Add(this.ParseExpression());
        }

        var fields = ImmutableArray.CreateBuilder<Field>();
        this.Expect(TokenKind.CurlyOpen);
        while (!this.Matches(TokenKind.CurlyClose))
        {
            var field = this.ParseField();
            fields.Add(field);
        }

        return new(doc, name, genericParams.ToImmutable(), bases.ToImmutable(), fields.ToImmutable());
    }

    private Field ParseField()
    {
        // Eat irrelevant modifiers
        this.Matches(TokenKind.KeywordReadonly);

        var peek = this.Peek();
        if (IsNameLike(peek.Kind)) return this.ParseSimpleField();
        if (peek.Kind == TokenKind.BracketOpen) return this.ParseIndexSignature();

        throw new InvalidOperationException($"unexpected token {peek}");
    }

    private SimpleField ParseSimpleField()
    {
        var nameToken = this.ParseNameToken();

        var doc = nameToken.LeadingComment;
        var name = nameToken.Text;

        var nullable = this.Matches(TokenKind.QuestionMark);

        this.Expect(TokenKind.Colon);

        var type = this.ParseExpression();
        // Optionally eat semicolon
        this.Matches(TokenKind.Semicolon);

        return new(doc, name, nullable, type);
    }

    private IndexSignature ParseIndexSignature()
    {
        this.Expect(TokenKind.BracketOpen);

        var keyName = this.Expect(TokenKind.Name).Text;

        this.Expect(TokenKind.Colon);
        var keyType = this.ParseExpression();

        this.Expect(TokenKind.BracketClose);

        this.Expect(TokenKind.Colon);
        var valueType = this.ParseExpression();

        // Optionally eat semicolon
        this.Matches(TokenKind.Semicolon);

        return new(keyName, keyType, valueType);
    }

    private string? ParseDocumentedPreamble(TokenKind keyword, params TokenKind[] qualifiers)
    {
        var doc = null as string;
        var first = true;
        while (true)
        {
            var token = this.Peek();
            if (first)
            {
                doc = token.LeadingComment;
                first = false;
            }
            if (token.Kind == keyword)
            {
                this.Advance();
                break;
            }
            // Just skip
            if (qualifiers.Contains(token.Kind))
            {
                this.Advance();
                continue;
            }
            // Error
            throw new InvalidOperationException($"illegal token {token}, expected {keyword} or one of its qualifiers");
        }
        return doc;
    }

    private string ParseName() => this.ParseNameToken().Text;

    private Token ParseNameToken()
    {
        var peek = this.Peek();
        if (!IsNameLike(peek.Kind)) throw new InvalidOperationException($"expected name, but got {peek}");
        return this.Advance();
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
        if (this.Peek().Kind == type)
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

    private static bool IsNameLike(TokenKind kind) => kind
        is TokenKind.Name
        or TokenKind.KeywordConst
        or TokenKind.KeywordEnum
        or TokenKind.KeywordExport
        or TokenKind.KeywordExtends
        or TokenKind.KeywordInterface
        or TokenKind.KeywordNamespace
        or TokenKind.KeywordReadonly
        or TokenKind.KeywordType;
}
