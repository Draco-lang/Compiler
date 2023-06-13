using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Utilities;

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
        private readonly IEnumerable<Internal.Syntax.SyntaxTrivia> triviaToAdd;

        public AddLeadingTriviaRewriter(IEnumerable<Internal.Syntax.SyntaxTrivia> triviaToAdd)
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

    private static TNode WithLeadingTrivia<TNode>(TNode node, IEnumerable<Internal.Syntax.SyntaxTrivia> trivia)
        where TNode : SyntaxNode
    {
        var rewriter = new AddLeadingTriviaRewriter(trivia);
        var green = node.Green.Accept(rewriter);
        return (TNode)green.ToRedNode(null!, null);
    }

    private static IEnumerable<Internal.Syntax.SyntaxTrivia> CreateCommentBlockTrivia(string prefix, string docs)
    {
        foreach (var (line, newline) in StringUtils.SplitIntoLines(docs))
        {
            yield return Internal.Syntax.SyntaxTrivia.From(TriviaKind.DocumentationComment, $"{prefix}{line}");
            if (newline is not null)
            {
                yield return Internal.Syntax.SyntaxTrivia.From(TriviaKind.Newline, newline);
            }
        }
    }

    public static TNode WithComments<TNode>(TNode node, string docs)
        where TNode : SyntaxNode =>
        WithLeadingTrivia(node, CreateCommentBlockTrivia("//", docs));

    public static TNode WithDocumentation<TNode>(TNode node, string docs)
        where TNode : SyntaxNode =>
        WithLeadingTrivia(node, CreateCommentBlockTrivia("///", docs));

    public static SyntaxToken Missing(TokenKind kind) =>
        Internal.Syntax.SyntaxToken.From(kind, string.Empty).ToRedNode(null!, null);

    public static SyntaxToken Name(string text) => MakeToken(TokenKind.Identifier, text);
    public static SyntaxToken Integer(int value) => MakeToken(TokenKind.LiteralInteger, value.ToString(), value);
    public static SyntaxToken? VisibilityToken(Visibility visibility) => visibility switch
    {
        Visibility.Private => null,
        Visibility.Internal => MakeToken(TokenKind.KeywordInternal),
        Visibility.Public => MakeToken(TokenKind.KeywordPublic),
        _ => throw new ArgumentOutOfRangeException(nameof(visibility)),
    };

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

    public static SeparatedSyntaxList<GenericParameterSyntax> GenericParameterList(IEnumerable<GenericParameterSyntax> parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static SeparatedSyntaxList<GenericParameterSyntax> GenericParameterList(params GenericParameterSyntax[] parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static GenericParameterSyntax GenericParameter(string name) => GenericParameter(Name(name));

    public static CompilationUnitSyntax CompilationUnit(IEnumerable<DeclarationSyntax> decls) =>
        CompilationUnit(SyntaxList(decls), EndOfInput);
    public static CompilationUnitSyntax CompilationUnit(params DeclarationSyntax[] decls) =>
        CompilationUnit(SyntaxList(decls), EndOfInput);

    public static ModuleDeclarationSyntax ModuleDeclaration(string name, SyntaxList<DeclarationSyntax> declarations) =>
        ModuleDeclaration(Module, Name(name), OpenBrace, declarations, CloseBrace);

    public static ImportDeclarationSyntax ImportDeclaration(string root, params string[] path) => ImportDeclaration(
        Import,
        path.Aggregate(
            RootImportPath(Name(root)) as ImportPathSyntax,
            (path, member) => MemberImportPath(path, Dot, Name(member))),
        Semicolon);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        string name,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            Visibility.Private,
            name,
            null,
            parameters,
            returnType,
            body);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        Visibility visibility,
        string name,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            visibility,
            name,
            null,
            parameters,
            returnType,
            body);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        string name,
        SeparatedSyntaxList<GenericParameterSyntax>? generics,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            Visibility.Private,
            name,
            generics,
            parameters,
            returnType,
            body);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        Visibility visibility,
        string name,
        SeparatedSyntaxList<GenericParameterSyntax>? generics,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            VisibilityToken(visibility),
            Func,
            Name(name),
            generics is null ? null : GenericParameterList(LessThan, generics, GreaterThan),
            OpenParen,
            parameters,
            CloseParen,
            returnType is null ? null : TypeSpecifier(Colon, returnType),
            body);

    public static VariableDeclarationSyntax VariableDeclaration(
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(null, true, name, type, value);

    public static VariableDeclarationSyntax VariableDeclaration(
        Visibility visibility,
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(VisibilityToken(visibility), true, name, type, value);

    public static VariableDeclarationSyntax ImmutableVariableDeclaration(
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(null, false, name, type, value);

    public static VariableDeclarationSyntax ImmutableVariableDeclaration(
        Visibility visibility,
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(VisibilityToken(visibility), false, name, type, value);

    public static VariableDeclarationSyntax VariableDeclaration(
        SyntaxToken? visibility,
        bool isMutable,
        string name,
        TypeSyntax? type = null,
        ExpressionSyntax? value = null) => VariableDeclaration(
        visibility,
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

    public static MemberExpressionSyntax MemberExpression(
        ExpressionSyntax accessed,
        string member) => MemberExpression(accessed, Dot, Name(member));
    public static MemberTypeSyntax MemberType(
        TypeSyntax accessed,
        string member) => MemberType(accessed, Dot, Name(member));

    public static GenericExpressionSyntax GenericExpression(
        ExpressionSyntax instantiated,
        params TypeSyntax[] typeParameters) => GenericExpression(
            instantiated,
            LessThan,
            SeparatedSyntaxList(Comma, typeParameters),
            GreaterThan);
    public static GenericTypeSyntax GenericType(
        TypeSyntax instantiated,
        params TypeSyntax[] typeParameters) => GenericType(
            instantiated,
            LessThan,
            SeparatedSyntaxList(Comma, typeParameters),
            GreaterThan);

    public static IndexExpressionSyntax IndexExpression(ExpressionSyntax indexed, SeparatedSyntaxList<ExpressionSyntax> indices) => IndexExpression(indexed, OpenBracket, indices, CloseBracket);
    public static IndexExpressionSyntax IndexExpression(ExpressionSyntax indexed, params ExpressionSyntax[] indices) => IndexExpression(indexed, SeparatedSyntaxList(Comma, indices));

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
    public static SyntaxToken Dot { get; } = MakeToken(TokenKind.Dot);
    public static SyntaxToken Semicolon { get; } = MakeToken(TokenKind.Semicolon);
    public static SyntaxToken Import { get; } = MakeToken(TokenKind.KeywordImport);
    public static SyntaxToken Return { get; } = MakeToken(TokenKind.KeywordReturn);
    public static SyntaxToken If { get; } = MakeToken(TokenKind.KeywordIf);
    public static SyntaxToken While { get; } = MakeToken(TokenKind.KeywordWhile);
    public static SyntaxToken Else { get; } = MakeToken(TokenKind.KeywordElse);
    public static SyntaxToken Var { get; } = MakeToken(TokenKind.KeywordVar);
    public static SyntaxToken Val { get; } = MakeToken(TokenKind.KeywordVal);
    public static SyntaxToken Func { get; } = MakeToken(TokenKind.KeywordFunc);
    public static SyntaxToken Goto { get; } = MakeToken(TokenKind.KeywordGoto);
    public static SyntaxToken Module { get; } = MakeToken(TokenKind.KeywordModule);
    public static SyntaxToken True { get; } = MakeToken(TokenKind.KeywordTrue, true);
    public static SyntaxToken False { get; } = MakeToken(TokenKind.KeywordFalse, false);
    public static SyntaxToken OpenBrace { get; } = MakeToken(TokenKind.CurlyOpen);
    public static SyntaxToken CloseBrace { get; } = MakeToken(TokenKind.CurlyClose);
    public static SyntaxToken OpenParen { get; } = MakeToken(TokenKind.ParenOpen);
    public static SyntaxToken CloseParen { get; } = MakeToken(TokenKind.ParenClose);
    public static SyntaxToken OpenBracket { get; } = MakeToken(TokenKind.BracketOpen);
    public static SyntaxToken CloseBracket { get; } = MakeToken(TokenKind.BracketClose);
    public static SyntaxToken Plus { get; } = MakeToken(TokenKind.Plus);
    public static SyntaxToken PlusAssign { get; } = MakeToken(TokenKind.PlusAssign);
    public static SyntaxToken LessThan { get; } = MakeToken(TokenKind.LessThan);
    public static SyntaxToken GreaterThan { get; } = MakeToken(TokenKind.GreaterThan);
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
