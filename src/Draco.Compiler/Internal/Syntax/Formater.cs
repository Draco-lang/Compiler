using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Token = Draco.Compiler.Internal.Syntax.ParseTree.Token;

namespace Draco.Compiler.Internal.Syntax;

internal class Formater : ParseTreeTransformerBase
{
    public ParseTree Format(ParseTree tree)
    {
        return this.Transform(tree, out bool changed);
    }
    public override Token TransformToken(Token token, out bool changed)
    {
        Token newToken = token.Type switch
        {
            TokenType.Assign or TokenType.Colon or TokenType.Comma or TokenType.Equal or
            TokenType.GreaterEqual or TokenType.GreaterThan or TokenType.Identifier or
            TokenType.InterpolationStart or TokenType.KeywordAnd or TokenType.KeywordElse or
            TokenType.KeywordFalse or TokenType.KeywordFrom or TokenType.KeywordFunc or
            TokenType.KeywordGoto or TokenType.KeywordIf or TokenType.KeywordImport or
            TokenType.KeywordMod or TokenType.KeywordNot or TokenType.KeywordOr or
            TokenType.KeywordRem or TokenType.KeywordReturn or TokenType.KeywordTrue or
            TokenType.KeywordVal or TokenType.KeywordVar or TokenType.KeywordWhile or
            TokenType.LessEqual or TokenType.LessThan or TokenType.Minus or
            TokenType.MinusAssign or TokenType.NotEqual or TokenType.Plus or
            TokenType.PlusAssign or TokenType.Slash or TokenType.SlashAssign or
            TokenType.Star or TokenType.StarAssign
            => token.NewTrailingTrivia(TokenType.Whitespace, " "),
            _ => token
        };
        changed = true;
        if (token.TrailingTrivia.Length != newToken.TrailingTrivia.Length) return newToken;
        for (int i = 0; i < token.TrailingTrivia.Length; i++)
        {
            if (token.TrailingTrivia[i].Text != newToken.TrailingTrivia[i].Text) return newToken;
        }
        changed = false;
        return token;
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
}
