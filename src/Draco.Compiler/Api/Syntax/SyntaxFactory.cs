using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.RedGreenTree.Attributes;
using static Draco.Compiler.Api.Syntax.ParseNode;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Factory functions for constructing a <see cref="ParseNode"/>.
/// </summary>
[SyntaxFactory(typeof(Internal.Syntax.ParseNode), typeof(ParseNode))]
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

    public static Decl.Func FuncDecl(Token name, Enclosed<PunctuatedList<FuncParam>> @params, TypeExpr? returnType, FuncBody body) => FuncDecl(
        KeywordFunc,
        name,
        @params,
        returnType is null ? null : TypeSpecifier(Colon, returnType),
        body);

    public static Decl.Func FuncDecl(Token name, ImmutableArray<FuncParam> @params, TypeExpr? returnType, FuncBody body) => FuncDecl(
        name,
        Enclosed(ParenOpen, PunctuatedList(@params, Comma, trailing: false), ParenClose),
        returnType,
        body);

    public static Decl.Variable VariableDecl(Token name, TypeExpr? type = null, Expr? value = null) => VariableDecl(
        KeywordVar,
        name,
        type is null ? null : TypeSpecifier(Colon, type),
        value is null ? null : ValueInitializer(Assign, value),
        Semicolon);

    public static Decl.Label LabelDecl(string name) => LabelDecl(Name(name), Colon);

    public static Enclosed<PunctuatedList<FuncParam>> FuncParamList(ImmutableArray<FuncParam> ps) => Enclosed(
        ParenOpen,
        PunctuatedList(ps, Comma, trailing: false),
        ParenClose);
    public static Enclosed<PunctuatedList<FuncParam>> FuncParamList(IEnumerable<FuncParam> ps) => FuncParamList(ps.ToImmutableArray());
    public static Enclosed<PunctuatedList<FuncParam>> FuncParamList(params FuncParam[] ps) => FuncParamList(ps.ToImmutableArray());

    public static FuncParam FuncParam(Token name, TypeExpr type) => FuncParam(name, TypeSpecifier(Colon, type));
    public static FuncBody.InlineBody InlineBodyFuncBody(Expr expr) => InlineBodyFuncBody(Assign, expr, Semicolon);

    public static Stmt.Expr ExprStmt(Expr expr) => ExprStmt(expr, null);

    public static Expr.Block BlockExpr(ImmutableArray<Stmt> stmts, Expr? value = null) => BlockExpr(new Enclosed<BlockContents>(
        OpenToken: CurlyOpen,
        Value: BlockContents(stmts, value),
        CloseToken: CurlyClose));
    public static Expr.Block BlockExpr(IEnumerable<Stmt> stmts, Expr? value = null) => BlockExpr(stmts.ToImmutableArray(), value);
    public static Expr.Block BlockExpr(params Stmt[] stmts) => BlockExpr(stmts.ToImmutableArray(), null);

    public static Expr.If IfExpr(Expr condition, Expr then, Expr? @else = null) => IfExpr(
        ifKeyword: KeywordIf,
        condition: Enclosed(ParenOpen, condition, ParenClose),
        then: then,
        @else: @else is null ? null : ElseClause(KeywordElse, @else));

    public static Expr.While WhileExpr(Expr condition, Expr body) => WhileExpr(
        whileKeyword: KeywordIf,
        condition: Enclosed(ParenOpen, condition, ParenClose),
        expression: body);

    public static Expr.Call CallExpr(Expr called, ImmutableArray<Expr> args) => CallExpr(
        called,
        Enclosed(ParenOpen, PunctuatedList(args, Comma, trailing: false), ParenClose));
    public static Expr.Call CallExpr(Expr called, IEnumerable<Expr> args) => CallExpr(called, args.ToImmutableArray());
    public static Expr.Call CallExpr(Expr called, params Expr[] args) => CallExpr(called, args.ToImmutableArray());

    public static Expr.Return ReturnExpr(Expr? value = null) => ReturnExpr(KeywordReturn, value);
    public static Expr.Goto GotoExpr(string label) => GotoExpr(KeywordGoto, NameExpr(label));

    public static Expr.Name NameExpr(string name) => NameExpr(Name(name));
    public static Expr.Literal LiteralExpr(int value) => LiteralExpr(Integer(value));
    public static Expr.Literal LiteralExpr(bool value) => LiteralExpr(value ? KeywordTrue : KeywordFalse);
    public static Expr.String StringExpr(string value) =>
        StringExpr(LineStringStart, ImmutableArray.Create<StringPart>(ContentStringPart(value)), LineStringEnd);

    public static StringPart.Content ContentStringPart(string value) => new(
        tree: null!,
        parent: null,
        green: new Internal.Syntax.ParseNode.StringPart.Content(
            Value: Internal.Syntax.ParseNode.Token.From(TokenType.StringContent, value),
            Diagnostics: ImmutableArray<Internal.Diagnostics.Diagnostic>.Empty));
}

// Utilities
public static partial class SyntaxFactory
{
    public static Token EndOfInput { get; } = MakeToken(TokenType.EndOfInput);
    public static Token Assign { get; } = MakeToken(TokenType.Assign);
    public static Token Comma { get; } = MakeToken(TokenType.Comma);
    public static Token Colon { get; } = MakeToken(TokenType.Colon);
    public static Token Semicolon { get; } = MakeToken(TokenType.Semicolon);
    public static Token KeywordReturn { get; } = MakeToken(TokenType.KeywordReturn);
    public static Token KeywordIf { get; } = MakeToken(TokenType.KeywordIf);
    public static Token KeywordElse { get; } = MakeToken(TokenType.KeywordElse);
    public static Token KeywordVar { get; } = MakeToken(TokenType.KeywordVar);
    public static Token KeywordVal { get; } = MakeToken(TokenType.KeywordVal);
    public static Token KeywordFunc { get; } = MakeToken(TokenType.KeywordFunc);
    public static Token KeywordGoto { get; } = MakeToken(TokenType.KeywordGoto);
    public static Token KeywordTrue { get; } = MakeToken(TokenType.KeywordTrue);
    public static Token KeywordFalse { get; } = MakeToken(TokenType.KeywordFalse);
    public static Token CurlyOpen { get; } = MakeToken(TokenType.CurlyOpen);
    public static Token CurlyClose { get; } = MakeToken(TokenType.CurlyClose);
    public static Token ParenOpen { get; } = MakeToken(TokenType.ParenOpen);
    public static Token ParenClose { get; } = MakeToken(TokenType.ParenClose);
    public static Token Plus { get; } = MakeToken(TokenType.Plus);
    public static Token LineStringStart { get; } = MakeToken(TokenType.LineStringStart, "\"");
    public static Token LineStringEnd { get; } = MakeToken(TokenType.LineStringEnd, "\"");

    private static Token MakeToken(TokenType tokenType) =>
        ToRed(tree: null!, parent: null, token: Internal.Syntax.ParseNode.Token.From(tokenType));
    private static Token MakeToken(TokenType tokenType, string text) =>
        ToRed(tree: null!, parent: null, token: Internal.Syntax.ParseNode.Token.From(tokenType, text));
    private static Token MakeToken(TokenType tokenType, string text, object? value) =>
        ToRed(tree: null!, parent: null, token: Internal.Syntax.ParseNode.Token.From(tokenType, text, value));
}

// Plumbing methods
public static partial class SyntaxFactory
{
    [return: NotNullIfNotNull(nameof(tree))]
    private static Internal.Syntax.ParseNode? ToGreen(ParseNode? tree) => tree?.Green;
    [return: NotNullIfNotNull(nameof(token))]
    private static Internal.Syntax.ParseNode.Token? ToGreen(Token? token) => token?.Green;
    private static ImmutableArray<Internal.Syntax.ParseNode.Decl> ToGreen(ImmutableArray<Decl> decls) =>
        decls.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseNode.ComparisonElement> ToGreen(ImmutableArray<ComparisonElement> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseNode.StringPart> ToGreen(ImmutableArray<StringPart> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static ImmutableArray<Internal.Syntax.ParseNode.Stmt> ToGreen(ImmutableArray<Stmt> elements) =>
        elements.Select(d => d.Green).ToImmutableArray();
    private static Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.Expr> ToGreen(Enclosed<Expr> enclosed) =>
        new(enclosed.OpenToken.Green, enclosed.Value.Green, enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.BlockContents> ToGreen(Enclosed<BlockContents> enclosed) =>
        new(enclosed.OpenToken.Green, enclosed.Value.Green, enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.FuncParam>> ToGreen(Enclosed<PunctuatedList<FuncParam>> enclosed) =>
        new(enclosed.OpenToken.Green, ToGreen(enclosed.Value), enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseNode.Enclosed<Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.Expr>> ToGreen(Enclosed<PunctuatedList<Expr>> enclosed) =>
        new(enclosed.OpenToken.Green, ToGreen(enclosed.Value), enclosed.CloseToken.Green);
    private static Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.FuncParam> ToGreen(PunctuatedList<FuncParam> elements) =>
        new(elements.Elements.Select(ToGreen).ToImmutableArray());
    private static Internal.Syntax.ParseNode.PunctuatedList<Internal.Syntax.ParseNode.Expr> ToGreen(PunctuatedList<Expr> elements) =>
        new(elements.Elements.Select(ToGreen).ToImmutableArray());
    private static Internal.Syntax.ParseNode.Punctuated<Internal.Syntax.ParseNode.FuncParam> ToGreen(Punctuated<FuncParam> punctuated) =>
        new(punctuated.Value.Green, punctuated.Punctuation?.Green);
    private static Internal.Syntax.ParseNode.Punctuated<Internal.Syntax.ParseNode.Expr> ToGreen(Punctuated<Expr> punctuated) =>
        new(punctuated.Value.Green, punctuated.Punctuation?.Green);
}
