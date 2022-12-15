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
    public static Token Name(string text) => MakeToken(TokenType.Identifier, text);
    public static Token Integer(int value) => MakeToken(TokenType.LiteralInteger, value.ToString(), value);

    public static Enclosed<T> Enclosed<T>(Token open, T value, Token close) => new(open, value, close);
    public static Punctuated<T> Punctuated<T>(T value, Token? punct) => new(value, punct);
    public static PunctuatedList<T> PunctuatedList<T>(ImmutableArray<Punctuated<T>> elements) => new(elements);
    public static PunctuatedList<T> PunctuatedList<T>(IEnumerable<Punctuated<T>> elements) => PunctuatedList(elements.ToImmutableArray());
    public static PunctuatedList<T> PunctuatedList<T>(params Punctuated<T>[] elements) => PunctuatedList(elements.ToImmutableArray());
    public static PunctuatedList<T> PunctuatedList<T>(ImmutableArray<T> elements, Token punctuation, bool trailing = true) => trailing
        ? PunctuatedList(elements.Select(e => Punctuated(e, punctuation)))
        : elements.Length == 0
            ? PunctuatedList(ImmutableArray<Punctuated<T>>.Empty)
            : PunctuatedList(elements.SkipLast(1).Select(e => Punctuated(e, punctuation)).Append(Punctuated(elements[^1], null)));

    public static CompilationUnit CompilationUnit(ImmutableArray<Decl> decls) => CompilationUnit(decls, EndOfInput);
    public static CompilationUnit CompilationUnit(IEnumerable<Decl> decls) => CompilationUnit(decls.ToImmutableArray());
    public static CompilationUnit CompilationUnit(params Decl[] decls) => CompilationUnit(decls.ToImmutableArray());

    public static Decl.Func FuncDecl(Token name, ImmutableArray<FuncParam> @params, TypeExpr? returnType, FuncBody body) => FuncDecl(
        KeywordFunc,
        name,
        Enclosed(ParenOpen, PunctuatedList(@params, Comma, trailing: false), ParenClose),
        returnType is null ? null : TypeSpecifier(Colon, returnType),
        body);

    public static Decl.Variable VariableDecl(Token name, TypeExpr? type = null, Expr? value = null) => VariableDecl(
        KeywordVar,
        name,
        type is null ? null : TypeSpecifier(Colon, type),
        value is null ? null : ValueInitializer(Assign, value),
        Semicolon);

    public static FuncParam FuncParam(Token name, TypeExpr type) => FuncParam(name, TypeSpecifier(Colon, type));

    public static Stmt.Expr ExprStmt(Expr expr) => ExprStmt(expr, null);

    public static Expr.Block BlockExpr(ImmutableArray<Stmt> stmts, Expr? value = null) => BlockExpr(new Enclosed<BlockContents>(
        OpenToken: CurlyOpen,
        Value: BlockContents(stmts, value),
        CloseToken: CurlyClose));
    public static Expr.Block BlockExpr(IEnumerable<Stmt> stmts, Expr? value = null) => BlockExpr(stmts.ToImmutableArray(), value);
    public static Expr.Block BlockExpr(params Stmt[] stmts) => BlockExpr(stmts.ToImmutableArray(), null);

    public static Expr.Name NameExpr(string name) => NameExpr(Name(name));
    public static Expr.Literal LiteralExpr(int value) => LiteralExpr(Integer(value));
}

// Utilities
public static partial class SyntaxFactory
{
    public static Token EndOfInput { get; } = MakeToken(TokenType.EndOfInput);
    public static Token Assign { get; } = MakeToken(TokenType.Assign);
    public static Token Comma { get; } = MakeToken(TokenType.Comma);
    public static Token Colon { get; } = MakeToken(TokenType.Colon);
    public static Token Semicolon { get; } = MakeToken(TokenType.Semicolon);
    public static Token KeywordVar { get; } = MakeToken(TokenType.KeywordVar);
    public static Token KeywordVal { get; } = MakeToken(TokenType.KeywordVal);
    public static Token KeywordFunc { get; } = MakeToken(TokenType.KeywordFunc);
    public static Token CurlyOpen { get; } = MakeToken(TokenType.CurlyOpen);
    public static Token CurlyClose { get; } = MakeToken(TokenType.CurlyClose);
    public static Token ParenOpen { get; } = MakeToken(TokenType.ParenOpen);
    public static Token ParenClose { get; } = MakeToken(TokenType.ParenClose);
    public static Token Plus { get; } = MakeToken(TokenType.Plus);

    private static Token MakeToken(TokenType tokenType) =>
        ToRed(parent: null, token: Internal.Syntax.ParseTree.Token.From(tokenType));
    private static Token MakeToken(TokenType tokenType, string text) =>
        ToRed(parent: null, token: Internal.Syntax.ParseTree.Token.From(tokenType, text));
    private static Token MakeToken(TokenType tokenType, string text, object? value) =>
        ToRed(parent: null, token: Internal.Syntax.ParseTree.Token.From(tokenType, text, value));
}

// Plumbing methods
public static partial class SyntaxFactory
{
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
