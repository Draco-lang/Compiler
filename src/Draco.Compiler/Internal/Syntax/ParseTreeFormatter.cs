using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Token = Draco.Compiler.Internal.Syntax.ParseTree.Token;

namespace Draco.Compiler.Internal.Syntax;

internal sealed record class ParseTreeFormatterSettings(string Indentation)
{
    internal static readonly ParseTreeFormatterSettings DefaultSettings = new ParseTreeFormatterSettings("    ");
}

internal sealed class ParseTreeFormatter : ParseTreeTransformerBase
{
    private TokenType? lastToken;
    private TokenType? nextToken;
    private IEnumerator<Token>? tokens;
    private readonly ParseTreeFormatterSettings settings;
    private ImmutableArray<Token> oneSpaceTrivia = CreateTrivia(TokenType.Whitespace, " ");
    private ImmutableArray<Token> noSpaceTrivia = CreateTrivia(TokenType.Whitespace, "");
    private ImmutableArray<Token> indentationTrivia;
    private ImmutableArray<Token> newlineTrivia = CreateTrivia(TokenType.Newline, Environment.NewLine);
    private int indentCount = 0;
    private string Indentation
    {
        get
        {
            var result = new StringBuilder();
            for (int i = 0; i < this.indentCount; ++i) result.Append(this.settings.Indentation);
            return result.ToString();
        }
    }

    internal ParseTreeFormatter(ParseTreeFormatterSettings settings)
    {
        this.settings = settings;
    }

    private IEnumerable<Token> GetTokens(ParseTree tree) =>
        tree.InOrderTraverse().OfType<Token>();

    private Token.Builder AddIndentation(Token.Builder newToken)
    {
        this.indentCount++;
        return newToken;
    }

    private Token.Builder RemoveIndentation(Token.Builder newToken)
    {
        this.indentCount--;
        return newToken;
    }

    public ParseTree Format(ParseTree tree)
    {
        this.indentationTrivia = CreateTrivia(TokenType.Whitespace, this.Indentation);
        this.tokens = this.GetTokens(tree).GetEnumerator();
        // We need to be one token ahead, because the next token affects the current one, so we must advance twice here
        if (!(this.tokens.MoveNext() && this.tokens.MoveNext())) return tree;
        this.nextToken = this.tokens.Current.Type;
        return this.Transform(tree, out _);
    }

    public override Token TransformToken(Token token, out bool changed)
    {
        if (token.Type == TokenType.EndOfInput)
        {
            changed = false;
            return token;
        }
        var newToken = Token.Builder.From(token);

        newToken = newToken.Type switch
        {
            // Maybe use dictionary in future to allow user to alter "stickiness" of some tokens
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.InterpolationStart or
            TokenType.KeywordAnd or TokenType.KeywordFrom or TokenType.KeywordImport or
            TokenType.KeywordMod or TokenType.KeywordNot or TokenType.KeywordOr or
            TokenType.KeywordRem or TokenType.LessEqual or TokenType.LessThan or
            TokenType.Minus or TokenType.MinusAssign or TokenType.NotEqual or
            TokenType.Plus or TokenType.PlusAssign or TokenType.Slash or
            TokenType.SlashAssign or TokenType.Star or TokenType.StarAssign
            => newToken.SetTrailingTrivia(this.oneSpaceTrivia),

            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordFunc
            => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),

            TokenType.ParenOpen => newToken.SetTrailingTrivia(this.noSpaceTrivia),

            TokenType.ParenClose => this.nextToken switch
            {
                TokenType.ParenClose or TokenType.Semicolon => newToken.SetTrailingTrivia(this.noSpaceTrivia),
                _ => newToken.SetTrailingTrivia(this.oneSpaceTrivia)
            },

            TokenType.Semicolon => newToken.SetTrailingTrivia(this.newlineTrivia),

            TokenType.CurlyOpen => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => this.AddIndentation(newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.newlineTrivia)),
                _ => this.AddIndentation(newToken).SetTrailingTrivia(this.newlineTrivia)
            },

            TokenType.CurlyClose => this.RemoveIndentation(newToken).SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.newlineTrivia),

            TokenType.KeywordReturn => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),

            TokenType.KeywordGoto => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),

            TokenType.KeywordIf => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),
                _ => newToken.SetTrailingTrivia(this.oneSpaceTrivia)
            },

            TokenType.KeywordElse => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),
                _ => newToken.SetTrailingTrivia(this.oneSpaceTrivia)
            },

            TokenType.KeywordWhile => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),

            TokenType.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenType.KeywordVal or TokenType.KeywordVar, nextToken: TokenType.Colon }
                => newToken.SetTrailingTrivia(this.noSpaceTrivia),

                { lastToken: TokenType.KeywordFrom or TokenType.KeywordVal or TokenType.KeywordVar or TokenType.Colon }
                => newToken.SetTrailingTrivia(this.oneSpaceTrivia),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen, nextToken: TokenType.Assign } => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.oneSpaceTrivia),
                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen } => newToken.SetLeadingTrivia(this.indentationTrivia).SetTrailingTrivia(this.noSpaceTrivia),

                { nextToken: TokenType.Semicolon or TokenType.ParenOpen or TokenType.ParenClose } => newToken.SetTrailingTrivia(this.noSpaceTrivia),
                _ => newToken.SetTrailingTrivia(this.oneSpaceTrivia),
            },

            TokenType.LiteralInteger or TokenType.LiteralFloat => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => newToken.SetTrailingTrivia(this.noSpaceTrivia),
                _ => newToken.SetTrailingTrivia(this.oneSpaceTrivia)
            },
            _ => newToken
        };
        var resultToken = newToken.Build();
        this.lastToken = resultToken!.Type;
        this.tokens!.MoveNext();
        if (this.tokens.Current is null) this.nextToken = TokenType.EndOfInput;
        else this.nextToken = this.tokens.Current.Type;
        if (resultToken is not null)
        {
            changed = !this.CheckTriviaEqual(resultToken, token);
            return resultToken;
        }
        changed = false;
        return token;
    }

    private bool CheckTriviaEqual(Token tok1, Token tok2)
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

    private static ImmutableArray<Token> CreateTrivia(TokenType type, string text) =>
        ImmutableArray.Create(Token.From(type, text));
}
