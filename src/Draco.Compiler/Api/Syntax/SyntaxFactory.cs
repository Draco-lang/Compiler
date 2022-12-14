using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Api.Syntax.ParseTree;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Factory functions for constructing a <see cref="ParseTree"/>.
/// </summary>
[SyntaxFactory(typeof(Internal.Syntax.ParseTree), typeof(ParseTree))]
public static partial class SyntaxFactory
{
    public static Token EndOfInput { get; } = MakeToken(TokenType.EndOfInput);

    private static Token MakeToken(TokenType tokenType) =>
        ToRed(parent: null, token: Internal.Syntax.ParseTree.Token.From(tokenType));
    private static Token MakeToken(TokenType tokenType, string text) =>
        ToRed(parent: null, token: Internal.Syntax.ParseTree.Token.From(tokenType, text));

    // Plumbing methods
    [return: NotNullIfNotNull(nameof(tree))]
    private static Internal.Syntax.ParseTree? ToGreen(ParseTree? tree) => tree?.Green;
    [return: NotNullIfNotNull(nameof(token))]
    private static Internal.Syntax.ParseTree.Token? ToGreen(Token? token) => token?.Green;
    private static ImmutableArray<Internal.Syntax.ParseTree.Decl> ToGreen(ImmutableArray<Decl> decls) =>
        decls.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseTree.ComparisonElement> ToGreen(ImmutableArray<ComparisonElement> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseTree.StringPart> ToGreen(ImmutableArray<StringPart> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseTree.Stmt> ToGreen(ImmutableArray<Stmt> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.Expr> ToGreen(Enclosed<Expr> enclosed) =>
        new(enclosed.OpenToken.Green, enclosed.Value.Green, enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.BlockContents> ToGreen(Enclosed<BlockContents> enclosed) =>
        new(enclosed.OpenToken.Green, enclosed.Value.Green, enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.FuncParam>> ToGreen(Enclosed<PunctuatedList<FuncParam>> enclosed) =>
        new(enclosed.OpenToken.Green, ToGreen(enclosed.Value), enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseTree.Enclosed<Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.Expr>> ToGreen(Enclosed<PunctuatedList<Expr>> enclosed) =>
        new(enclosed.OpenToken.Green, ToGreen(enclosed.Value), enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.FuncParam> ToGreen(PunctuatedList<FuncParam> elements) =>
        new(elements.Elements.Select(ToGreen).ToImmutableArray());
    private static Internal.Syntax.ParseTree.PunctuatedList<Internal.Syntax.ParseTree.Expr> ToGreen(PunctuatedList<Expr> elements) =>
        new(elements.Elements.Select(ToGreen).ToImmutableArray());
    private static Internal.Syntax.ParseTree.Punctuated<Internal.Syntax.ParseTree.FuncParam> ToGreen(Punctuated<FuncParam> punctuated) =>
        new(punctuated.Value.Green, punctuated.Punctuation?.Green);
    private static Internal.Syntax.ParseTree.Punctuated<Internal.Syntax.ParseTree.Expr> ToGreen(Punctuated<Expr> punctuated) =>
        new(punctuated.Value.Green, punctuated.Punctuation?.Green);
}
