using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Utilities for constructing <see cref="SyntaxNode"/>s.
/// </summary>
public static partial class SyntaxFactory
{
    // REWRITERS ///////////////////////////////////////////////////////////////

    private sealed class AddLeadingTriviaRewriter : SyntaxRewriter
    {
        private bool firstToken = true;
        private readonly Internal.Syntax.SyntaxTrivia[] triviaToAdd;

        public AddLeadingTriviaRewriter(Internal.Syntax.SyntaxTrivia[] triviaToAdd)
        {
            this.triviaToAdd = triviaToAdd;
        }

        public override Internal.Syntax.SyntaxToken VisitSyntaxToken(Internal.Syntax.SyntaxToken node)
        {
            if (!this.firstToken) return node;
            this.firstToken = false;
            var builder = node.ToBuilder();
            builder.LeadingTrivia.AddRange(this.triviaToAdd);
            return builder.Build();
        }
    }

    // NODES ///////////////////////////////////////////////////////////////////

    private static TNode WithLeadingTrivia<TNode>(TNode node, params Internal.Syntax.SyntaxTrivia[] trivia)
        where TNode : SyntaxNode
    {
        var rewriter = new AddLeadingTriviaRewriter(trivia);
        var green = node.Green.Accept(rewriter);
        return (TNode)green.ToRedNode(null!, null);
    }

    public static TNode WithDocumentation<TNode>(TNode node, string docs)
        where TNode : SyntaxNode =>
        WithLeadingTrivia(node, docs.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(x => Internal.Syntax.SyntaxTrivia.From(TriviaKind.DocumentationComment, $"///{x}")).ToArray());

    public static TNode WithComments<TNode>(TNode node, string docs)
        where TNode : SyntaxNode =>
        WithLeadingTrivia(node, docs.Split(new string[] { "\r\n", "\n", "\r" }, StringSplitOptions.None)
            .Select(x => Internal.Syntax.SyntaxTrivia.From(TriviaKind.DocumentationComment, $"//{x}")).ToArray());

    public static SyntaxToken Name(string text) => MakeToken(TokenKind.Identifier, text);
    public static SyntaxToken Integer(int value) => MakeToken(TokenKind.LiteralInteger, value.ToString(), value);

    public static SyntaxList<TNode> SyntaxList<TNode>(IEnumerable<TNode> elements)
        where TNode : SyntaxNode => new(
            tree: null!,
            parent: null,
            green: Syntax.SyntaxList<TNode>.MakeGreen(elements.Select(n => n.Green)));
    public static SyntaxList<TNode> SyntaxList<TNode>(params TNode[] elements)
        where TNode : SyntaxNode => SyntaxList(elements.AsEnumerable());

    public static SeparatedSyntaxList<TNode> SeparatedSyntaxList<TNode>(SyntaxToken separator, IEnumerable<TNode> elements)
        where TNode : SyntaxNode => new(
            tree: null!,
            parent: null,
            green: Syntax.SeparatedSyntaxList<TNode>.MakeGreen(elements.SelectMany(n => new[] { n.Green, separator.Green })));
    public static SeparatedSyntaxList<TNode> SeparatedSyntaxList<TNode>(SyntaxToken separator, params TNode[] elements)
        where TNode : SyntaxNode => SeparatedSyntaxList(separator, elements.AsEnumerable());

    public static SeparatedSyntaxList<ParameterSyntax> ParameterList(IEnumerable<ParameterSyntax> parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static SeparatedSyntaxList<ParameterSyntax> ParameterList(params ParameterSyntax[] parameters) =>
        SeparatedSyntaxList(Comma, parameters);

    public static ParameterSyntax Parameter(string name, TypeSyntax type) => Parameter(Name(name), Colon, type);

    public static CompilationUnitSyntax CompilationUnit(IEnumerable<DeclarationSyntax> decls) =>
        CompilationUnit(SyntaxList(decls), EndOfInput);
    public static CompilationUnitSyntax CompilationUnit(params DeclarationSyntax[] decls) =>
        CompilationUnit(SyntaxList(decls), EndOfInput);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        string name,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            Func,
            Name(name),
            OpenParen,
            parameters,
            CloseParen,
            returnType is null ? null : TypeSpecifier(Colon, returnType),
            body);

    public static VariableDeclarationSyntax VariableDeclaration(
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(true, name, type, value);

    public static VariableDeclarationSyntax ImmutableVariableDeclaration(
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(false, name, type, value);

    public static VariableDeclarationSyntax VariableDeclaration(
        bool isMutable,
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(
        isMutable ? Var : Val,
        Name(name),
        type is null ? null : TypeSpecifier(Colon, type),
        value is null ? null : ValueSpecifier(Assign, value),
        Semicolon);

    public static LabelDeclarationSyntax LabelDeclaration(string name) => LabelDeclaration(Name(name), Colon);

    public static InlineFunctionBodySyntax InlineFunctionBody(ExpressionSyntax expr) => InlineFunctionBody(Assign, expr, Semicolon);

    public static BlockFunctionBodySyntax BlockFunctionBody(IEnumerable<StatementSyntax> stmts) => BlockFunctionBody(
        OpenBrace,
        SyntaxList(stmts),
        CloseBrace);
    public static BlockFunctionBodySyntax BlockFunctionBody(params StatementSyntax[] stmts) => BlockFunctionBody(stmts.AsEnumerable());

    public static ExpressionStatementSyntax ExpressionStatement(ExpressionSyntax expr) => ExpressionStatement(expr, null);
    public static BlockExpressionSyntax BlockExpression(
        IEnumerable<StatementSyntax> stmts,
        ExpressionSyntax? value = null) => BlockExpression(
        OpenBrace,
        SyntaxList(stmts),
        value,
        CloseBrace);
    public static BlockExpressionSyntax BlockExpression(params StatementSyntax[] stmts) => BlockExpression(stmts.AsEnumerable());

    public static IfExpressionSyntax IfExpression(
        ExpressionSyntax condition,
        ExpressionSyntax then,
        ExpressionSyntax? @else = null) => IfExpression(
        If,
        OpenParen,
        condition,
        CloseParen,
        then,
        @else is null ? null : ElseClause(Else, @else));

    public static WhileExpressionSyntax WhileExpression(
        ExpressionSyntax condition,
        ExpressionSyntax body) => WhileExpression(
        While,
        OpenParen,
        condition,
        CloseParen,
        body);

    public static CallExpressionSyntax CallExpression(
        ExpressionSyntax called,
        IEnumerable<ExpressionSyntax> args) => CallExpression(
        called,
        OpenParen,
        SeparatedSyntaxList(Comma, args),
        CloseParen);
    public static CallExpressionSyntax CallExpression(
        ExpressionSyntax called,
        params ExpressionSyntax[] args) => CallExpression(called, args.AsEnumerable());

    public static ReturnExpressionSyntax ReturnExpression(ExpressionSyntax? value = null) => ReturnExpression(Return, value);
    public static GotoExpressionSyntax GotoExpression(string label) => GotoExpression(Goto, NameLabel(Name(label)));

    public static NameTypeSyntax NameType(string name) => NameType(Name(name));
    public static NameExpressionSyntax NameExpression(string name) => NameExpression(Name(name));
    public static LiteralExpressionSyntax LiteralExpression(int value) => LiteralExpression(Integer(value));
    public static LiteralExpressionSyntax LiteralExpression(bool value) => LiteralExpression(value ? True : False);
    public static StringExpressionSyntax StringExpression(string value) =>
        StringExpression(LineStringStart, SyntaxList(TextStringPart(value) as StringPartSyntax), LineStringEnd);

    public static TextStringPartSyntax TextStringPart(string value) =>
        TextStringPart(MakeToken(TokenKind.StringContent, value, value));

    // TOKENS //////////////////////////////////////////////////////////////////

    public static SyntaxToken EndOfInput { get; } = MakeToken(TokenKind.EndOfInput);
    public static SyntaxToken Assign { get; } = MakeToken(TokenKind.Assign);
    public static SyntaxToken Comma { get; } = MakeToken(TokenKind.Comma);
    public static SyntaxToken Colon { get; } = MakeToken(TokenKind.Colon);
    public static SyntaxToken Semicolon { get; } = MakeToken(TokenKind.Semicolon);
    public static SyntaxToken Return { get; } = MakeToken(TokenKind.KeywordReturn);
    public static SyntaxToken If { get; } = MakeToken(TokenKind.KeywordIf);
    public static SyntaxToken While { get; } = MakeToken(TokenKind.KeywordWhile);
    public static SyntaxToken Else { get; } = MakeToken(TokenKind.KeywordElse);
    public static SyntaxToken Var { get; } = MakeToken(TokenKind.KeywordVar);
    public static SyntaxToken Val { get; } = MakeToken(TokenKind.KeywordVal);
    public static SyntaxToken Func { get; } = MakeToken(TokenKind.KeywordFunc);
    public static SyntaxToken Goto { get; } = MakeToken(TokenKind.KeywordGoto);
    public static SyntaxToken True { get; } = MakeToken(TokenKind.KeywordTrue, true);
    public static SyntaxToken False { get; } = MakeToken(TokenKind.KeywordFalse, false);
    public static SyntaxToken OpenBrace { get; } = MakeToken(TokenKind.CurlyOpen);
    public static SyntaxToken CloseBrace { get; } = MakeToken(TokenKind.CurlyClose);
    public static SyntaxToken OpenParen { get; } = MakeToken(TokenKind.ParenOpen);
    public static SyntaxToken CloseParen { get; } = MakeToken(TokenKind.ParenClose);
    public static SyntaxToken Plus { get; } = MakeToken(TokenKind.Plus);
    public static SyntaxToken LineStringStart { get; } = MakeToken(TokenKind.LineStringStart, "\"");
    public static SyntaxToken LineStringEnd { get; } = MakeToken(TokenKind.LineStringEnd, "\"");

    private static SyntaxToken MakeToken(TokenKind tokenKind) =>
        Internal.Syntax.SyntaxToken.From(tokenKind).ToRedNode(null!, null);
    private static SyntaxToken MakeToken(TokenKind tokenKind, string text) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, text).ToRedNode(null!, null);
    private static SyntaxToken MakeToken(TokenKind tokenKind, string text, object? value) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, text, value).ToRedNode(null!, null);
    private static SyntaxToken MakeToken(TokenKind tokenKind, object? value) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, value: value).ToRedNode(null!, null);
}
