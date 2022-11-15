using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Token = Draco.Compiler.Internal.Syntax.ParseTree.Token;

namespace Draco.Compiler.Internal.Syntax;

internal class Formater : ParseTreeTransformerBase
{
    private TokenType lastToken;
    private TokenType nextToken;
    private IEnumerator<Token> tokens;
    private int indentCount = 0;
    private string indentation
    {
        get
        {
            var oneIndent = "    ";
            var result = new StringBuilder();
            for (int i = 0; i < this.indentCount; i++, result.Append(oneIndent)) ;
            return result.ToString();
        }
    }
    private IEnumerable<Token> GetTokens(ParseTree tree)
    {
        var tokens = new List<Token>();
        foreach (var child in tree.Children)
        {
            if (child is ParseTree.Token tok) tokens.Add(tok);
            else tokens.AddRange(this.GetTokens(child));
        }
        return tokens;
    }

    private Token AddIndentation(Token token)
    {
        this.indentCount++;
        return token;
    }
    private Token RemoveIndentation(Token token)
    {
        this.indentCount--;
        return token;
    }
    public ParseTree Format(ParseTree tree)
    {
        this.tokens = this.GetTokens(tree).GetEnumerator();
        this.tokens.MoveNext();
        this.tokens.MoveNext();
        this.nextToken = this.tokens.Current.Type;
        return this.Transform(tree, out bool changed);
    }

    public override Token TransformToken(Token token, out bool changed)
    {
        // TODO: maybe change lastToken and nextToken to Token instead of TokenType?
        if (token.Type == TokenType.EndOfInput)
        {
            changed = false;
            return token;
        }
        Token? newToken = token.Type switch
        {
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.InterpolationStart or
            TokenType.KeywordAnd or TokenType.KeywordElse or TokenType.KeywordFrom or
            TokenType.KeywordGoto or TokenType.KeywordIf or TokenType.KeywordImport or
            TokenType.KeywordMod or TokenType.KeywordNot or
            TokenType.KeywordOr or TokenType.KeywordRem or TokenType.KeywordReturn or
            TokenType.KeywordWhile or TokenType.LessEqual or TokenType.LessThan or
            TokenType.Minus or TokenType.MinusAssign or TokenType.NotEqual or
            TokenType.Plus or TokenType.PlusAssign or TokenType.Slash or
            TokenType.SlashAssign or TokenType.Star or TokenType.StarAssign
            => token.NewTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordFunc
            => token.NewLeadingTrivia(TokenType.Whitespace, this.indentation).NewTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.ParenOpen => token.NewTrailingTrivia(TokenType.Whitespace, ""),

            TokenType.ParenClose => this.nextToken == TokenType.CurlyOpen ? token.NewTrailingTrivia(TokenType.Whitespace, " ") : token.NewTrailingTrivia(TokenType.Whitespace, ""),

            TokenType.Semicolon => token.NewTrailingTrivia(TokenType.Newline, Environment.NewLine),

            TokenType.CurlyOpen => this.AddIndentation(token).NewTrailingTrivia(TokenType.Newline, Environment.NewLine),

            TokenType.CurlyClose => this.RemoveIndentation(token).NewLeadingTrivia(TokenType.Whitespace, this.indentation),

            TokenType.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenType.KeywordVal or TokenType.KeywordVar, nextToken: TokenType.Colon }
                => token.NewTrailingTrivia(TokenType.Whitespace, ""),

                { lastToken: TokenType.KeywordFrom or TokenType.KeywordVal or
                TokenType.KeywordVar or TokenType.Colon }
                => token.NewTrailingTrivia(TokenType.Whitespace, " "),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen, nextToken: TokenType.Assign } => token.NewLeadingTrivia(TokenType.Whitespace, this.indentation).NewTrailingTrivia(TokenType.Whitespace, " "),
                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen } => token.NewLeadingTrivia(TokenType.Whitespace, this.indentation).NewTrailingTrivia(TokenType.Whitespace, ""),

                _ => token.NewTrailingTrivia(TokenType.Whitespace, ""),
            },

            TokenType.LiteralInteger or TokenType.LiteralFloat => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon } => token.NewTrailingTrivia(TokenType.Whitespace, ""),
                _ => token.NewTrailingTrivia(TokenType.Whitespace, " ")
            },
            _ => null
        };
        this.lastToken = token.Type;
        this.tokens.MoveNext();
        if (this.tokens.Current is null) this.nextToken = TokenType.EndOfInput;
        else this.nextToken = this.tokens.Current.Type;
        if (newToken is not null)
        {
            changed = !this.checkTokensValueEaqual(token, newToken);
            return newToken;
        }
        changed = false;
        return token;
    }

    private bool checkTokensValueEaqual(Token tok1, Token tok2)
    {
        if (tok1.TrailingTrivia.Length != tok2.TrailingTrivia.Length) return false;
        for (int i = 0; i < tok1.TrailingTrivia.Length; i++)
        {
            if (tok1.TrailingTrivia[i].Text != tok2.TrailingTrivia[i].Text) return false;
        }

        if (tok1.LeadingTrivia.Length != tok2.LeadingTrivia.Length) return false;
        for (int i = 0; i < tok1.LeadingTrivia.Length; i++)
        {
            if (tok1.LeadingTrivia[i].Text != tok2.LeadingTrivia[i].Text) return false;
        }
        return true;
    }
}

internal static class BuilderExtensions
{
    public static Token.Builder CreateBuilder(this Token token)
    {
        return new Token.Builder()
            .SetType(token.Type)
            .SetText(token.Text)
            .SetValue(token.Value);
    }

    public static Token NewTrailingTrivia(this Token token, TokenType trailingTriviaType, string trailingTriviaText)
    {
        return CreateBuilder(token).SetLeadingTrivia(token.LeadingTrivia).SetDiagnostics(token.Diagnostics).SetTrailingTrivia(ImmutableArray.Create(Token.From(trailingTriviaType, trailingTriviaText))).Build();
    }
    public static Token NewLeadingTrivia(this Token token, TokenType leadingTriviaType, string leadingTriviaText)
    {
        return CreateBuilder(token).SetTrailingTrivia(token.TrailingTrivia).SetDiagnostics(token.Diagnostics).SetLeadingTrivia(ImmutableArray.Create(Token.From(leadingTriviaType, leadingTriviaText))).Build();
    }
}
