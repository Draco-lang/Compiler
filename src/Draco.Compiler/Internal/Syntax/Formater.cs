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

enum TokenContext
{
    IdentifierSpaceAfter,
    IdentifierNothingAfter,
    CurlyOpenNewlineAfter,
    CurlyCloseNewlineBeforeAndAfter,
    ParenCloseNothingAfter,
}

internal class Formater : ParseTreeTransformerBase
{
    private readonly Queue<TokenContext> context = new Queue<TokenContext>();
    public ParseTree Format(ParseTree tree)
    {
        return this.Transform(tree, out bool changed);
    }

    // TODO: add stack that can hold the curent context e.g Should identifier have space after or not,
    // in specific overrides just add to this stack and then call base.VisitOverrideName (queue might be better then stack?)
    public override ParseTree.Decl.Variable TransformVariableDecl(ParseTree.Decl.Variable node, out bool changed)
    {
        // TODO: cover all variable decl cases
        if (node.Type is not null)
        {
            this.context.Enqueue(TokenContext.IdentifierNothingAfter);
            this.context.Enqueue(TokenContext.IdentifierSpaceAfter);
        }
        return base.TransformVariableDecl(node, out changed);
    }

    public override ParseTree.Decl.Func TransformFuncDecl(ParseTree.Decl.Func node, out bool changed)
    {
        // TODO: check for type anotation
        this.context.Enqueue(TokenContext.IdentifierNothingAfter);
        this.context.Enqueue(TokenContext.ParenCloseNothingAfter);
        this.context.Enqueue(TokenContext.CurlyOpenNewlineAfter);
        this.context.Enqueue(TokenContext.CurlyCloseNewlineBeforeAndAfter);
        return base.TransformFuncDecl(node, out changed);
    }

    public override Token TransformToken(Token token, out bool changed)
    {
        if(token.Type == TokenType.EndOfInput)
        {
            changed = false;
            return token;
        }
        Token? newToken = token.Type switch
        {
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.InterpolationStart or
            TokenType.KeywordAnd or TokenType.KeywordElse or TokenType.KeywordFrom or
            TokenType.KeywordFunc or TokenType.KeywordGoto or TokenType.KeywordIf or
            TokenType.KeywordImport or TokenType.KeywordMod or TokenType.KeywordNot or
            TokenType.KeywordOr or TokenType.KeywordRem or TokenType.KeywordReturn or
            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordWhile or
            TokenType.LessEqual or TokenType.LessThan or TokenType.Minus or
            TokenType.MinusAssign or TokenType.NotEqual or TokenType.Plus or
            TokenType.PlusAssign or TokenType.Slash or TokenType.SlashAssign or
            TokenType.Star or TokenType.StarAssign
            => token.NewTrailingTrivia(TokenType.Whitespace, " "),
            TokenType.ParenOpen => token.NewTrailingTrivia(TokenType.Whitespace, ""),
            _ => null
        };
        if (newToken is not null)
        {
            changed = this.checkTokensValueEaqual(token, newToken);
            return newToken;
        }
        if (!this.context.TryDequeue(out TokenContext currentContext)) throw new InvalidOperationException("Expected token context");
        newToken = currentContext switch
        {
            TokenContext.IdentifierSpaceAfter => token.NewTrailingTrivia(TokenType.Whitespace, " "),
            TokenContext.IdentifierNothingAfter => token.NewTrailingTrivia(TokenType.Whitespace, ""),
            TokenContext.CurlyOpenNewlineAfter => token.NewTrailingTrivia(TokenType.Newline, "\n"),
            TokenContext.CurlyCloseNewlineBeforeAndAfter => token.NewLeadingTrivia(TokenType.Newline, "\n").NewTrailingTrivia(TokenType.Newline, "\n"),
            TokenContext.ParenCloseNothingAfter => token.NewTrailingTrivia(TokenType.Whitespace, ""),
            _ => throw new ArgumentOutOfRangeException(nameof(currentContext))
        };
        changed = this.checkTokensValueEaqual(token, newToken);
        return newToken;
    }

    private bool checkTokensValueEaqual(Token tok1, Token tok2)
    {
        if (tok1.TrailingTrivia.Length != tok2.TrailingTrivia.Length) return false;
        for (int i = 0; i < tok1.TrailingTrivia.Length; i++)
        {
            if (tok1.TrailingTrivia[i].Text != tok2.TrailingTrivia[i].Text) return false;
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
