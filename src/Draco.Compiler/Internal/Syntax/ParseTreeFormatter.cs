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

internal sealed record class ParseTreeFormatterSettings(string Indentation);

internal class ParseTreeFormatter : ParseTreeTransformerBase
{
    private TokenType? lastToken;
    private TokenType? nextToken;
    private IEnumerator<Token>? tokens;
    private int indentCount = 0;
    private ParseTreeFormatterSettings settings;
    private string Indentation
    {
        get
        {
            var result = new StringBuilder();
            for (int i = 0; i < this.indentCount; i++, result.Append(this.settings.Indentation)) ;
            return result.ToString();
        }
    }

    internal ParseTreeFormatter(ParseTreeFormatterSettings settings)
    {
        this.settings = settings;
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
        this.tokens = this.GetTokens(tree).GetEnumerator();
        this.tokens.MoveNext();
        this.tokens.MoveNext();
        this.nextToken = this.tokens.Current.Type;
        return this.Transform(tree, out bool changed);
    }

    public override Token TransformToken(Token token, out bool changed)
    {
        Draco.Compiler.Internal.Diagnostics.Diagnostic diagnostic = Draco.Compiler.Internal.Diagnostics.Diagnostic.Create
            (SyntaxErrors.ExpectedToken, new Diagnostics.Location());

        Draco.Compiler.Internal.Diagnostics.Diagnostic diagnostic2 = Draco.Compiler.Internal.Diagnostics.Diagnostic.Create
            (SyntaxErrors.ExpectedToken, new Diagnostics.Location());

        var fdb = diagnostic == diagnostic2;
        if (token.Type == TokenType.EndOfInput)
        {
            changed = false;
            return token;
        }
        var newToken = Token.Builder.From(token);

        newToken = newToken.Type switch
        {
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.InterpolationStart or
            TokenType.KeywordAnd or TokenType.KeywordFrom or TokenType.KeywordImport or
            TokenType.KeywordMod or TokenType.KeywordNot or TokenType.KeywordOr or
            TokenType.KeywordRem or TokenType.LessEqual or TokenType.LessThan or
            TokenType.Minus or TokenType.MinusAssign or TokenType.NotEqual or
            TokenType.Plus or TokenType.PlusAssign or TokenType.Slash or
            TokenType.SlashAssign or TokenType.Star or TokenType.StarAssign
            => newToken.SetTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordFunc
            => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.ParenOpen => newToken.SetTrailingTrivia(TokenType.Whitespace, ""),

            TokenType.ParenClose => this.nextToken switch
            {
                TokenType.ParenClose or TokenType.Semicolon => newToken.SetTrailingTrivia(TokenType.Whitespace, ""),
                _ => newToken.SetTrailingTrivia(TokenType.Whitespace, " ")
            },

            TokenType.Semicolon => newToken.SetTrailingTrivia(TokenType.Newline, Environment.NewLine),

            TokenType.CurlyOpen => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => this.AddIndentation(newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Newline, Environment.NewLine)),
                _ => this.AddIndentation(newToken).SetTrailingTrivia(TokenType.Newline, Environment.NewLine)
            },

            TokenType.CurlyClose => this.RemoveIndentation(newToken).SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Newline, Environment.NewLine),

            TokenType.KeywordReturn => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.KeywordGoto => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.KeywordIf => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),
                _ => newToken.SetTrailingTrivia(TokenType.Whitespace, " ")
            },

            TokenType.KeywordElse => this.lastToken switch
            {
                TokenType.Semicolon or TokenType.CurlyClose => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),
                _ => newToken.SetTrailingTrivia(TokenType.Whitespace, " ")
            },

            TokenType.KeywordWhile => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),

            TokenType.Identifier => (this.lastToken, this.nextToken) switch
            {
                { lastToken: TokenType.KeywordVal or TokenType.KeywordVar, nextToken: TokenType.Colon }
                => newToken.SetTrailingTrivia(TokenType.Whitespace, ""),

                { lastToken: TokenType.KeywordFrom or TokenType.KeywordVal or TokenType.KeywordVar or TokenType.Colon }
                => newToken.SetTrailingTrivia(TokenType.Whitespace, " "),

                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen, nextToken: TokenType.Assign } => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, " "),
                { lastToken: TokenType.Semicolon or TokenType.CurlyOpen } => newToken.SetLeadingTrivia(TokenType.Whitespace, this.Indentation).SetTrailingTrivia(TokenType.Whitespace, ""),

                { nextToken: TokenType.Semicolon or TokenType.ParenOpen or TokenType.ParenClose } => newToken.SetTrailingTrivia(TokenType.Whitespace, ""),
                _ => newToken.SetTrailingTrivia(TokenType.Whitespace, " "),
            },

            TokenType.LiteralInteger or TokenType.LiteralFloat => (this.lastToken, this.nextToken) switch
            {
                { nextToken: TokenType.Semicolon or TokenType.ParenClose } => newToken.SetTrailingTrivia(TokenType.Whitespace, ""),
                _ => newToken.SetTrailingTrivia(TokenType.Whitespace, " ")
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
            changed = !this.checkTokensValueEqual(resultToken, token);
            return resultToken;
        }
        changed = false;
        return token;
    }

    private bool checkTokensValueEqual(Token tok1, Token tok2)
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
