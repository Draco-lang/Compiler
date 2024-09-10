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

    private sealed class AddLeadingTriviaRewriter(
        IEnumerable<Internal.Syntax.SyntaxTrivia> triviaToAdd) : SyntaxRewriter
    {
        private bool firstToken = true;

        public override Internal.Syntax.SyntaxToken VisitSyntaxToken(Internal.Syntax.SyntaxToken node)
        {
            if (!this.firstToken) return node;
            this.firstToken = false;
            var builder = node.ToBuilder();
            builder.LeadingTrivia.AddRange(triviaToAdd);
            return builder.Build();
        }
    }

    // NODES ///////////////////////////////////////////////////////////////////

    private static TNode WithLeadingTrivia<TNode>(TNode node, IEnumerable<Internal.Syntax.SyntaxTrivia> trivia)
        where TNode : SyntaxNode
    {
        var rewriter = new AddLeadingTriviaRewriter(trivia);
        var green = node.Green.Accept(rewriter);
        return (TNode)green.ToRedNode(null!, null, 0);
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
        Internal.Syntax.SyntaxToken.From(kind, string.Empty).ToRedNode(null!, null, 0);

    public static SyntaxToken Identifier(string text) => Token(TokenKind.Identifier, text);
    public static SyntaxToken Integer(int value) => Token(TokenKind.LiteralInteger, value.ToString(), value);

    public static SyntaxToken? VisibilityToken(Visibility visibility) => visibility switch
    {
        Visibility.Private => null,
        Visibility.Internal => Token(TokenKind.KeywordInternal),
        Visibility.Public => Token(TokenKind.KeywordPublic),
        _ => throw new ArgumentOutOfRangeException(nameof(visibility)),
    };

    public static SyntaxList<TNode> SyntaxList<TNode>(IEnumerable<TNode> elements)
        where TNode : SyntaxNode => new(
            tree: null!,
            parent: null,
            fullPosition: 0,
            green: Syntax.SyntaxList<TNode>.MakeGreen(elements.Select(n => n.Green)));
    public static SyntaxList<TNode> SyntaxList<TNode>(params TNode[] elements)
        where TNode : SyntaxNode => SyntaxList(elements.AsEnumerable());

    public static SeparatedSyntaxList<TNode> SeparatedSyntaxList<TNode>(SyntaxToken separator, IEnumerable<TNode> elements)
        where TNode : SyntaxNode => new(
            tree: null!,
            parent: null,
            fullPosition: 0,
            green: Syntax.SeparatedSyntaxList<TNode>.MakeGreen(elements.SelectMany(n => new[] { n.Green, separator.Green })));
    public static SeparatedSyntaxList<TNode> SeparatedSyntaxList<TNode>(SyntaxToken separator, params TNode[] elements)
        where TNode : SyntaxNode => SeparatedSyntaxList(separator, elements.AsEnumerable());

    public static SeparatedSyntaxList<ParameterSyntax> ParameterList(IEnumerable<ParameterSyntax> parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static SeparatedSyntaxList<ParameterSyntax> ParameterList(params ParameterSyntax[] parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static ParameterSyntax Parameter(string name, TypeSyntax type) =>
        Parameter([], name, type);
    public static ParameterSyntax Parameter(IEnumerable<AttributeSyntax> attributes, string name, TypeSyntax type) =>
        Parameter(SyntaxList(attributes), null, Identifier(name), Colon, type);
    public static ParameterSyntax VariadicParameter(string name, TypeSyntax type) =>
        Parameter(SyntaxList<AttributeSyntax>(), Ellipsis, Identifier(name), Colon, type);

    public static SeparatedSyntaxList<GenericParameterSyntax> GenericParameterList(IEnumerable<GenericParameterSyntax> parameters) =>
        SeparatedSyntaxList(Comma, parameters);
    public static SeparatedSyntaxList<GenericParameterSyntax> GenericParameterList(params GenericParameterSyntax[] parameters) =>
        SeparatedSyntaxList(Comma, parameters);

    public static CompilationUnitSyntax CompilationUnit(IEnumerable<DeclarationSyntax> decls) =>
        CompilationUnit(SyntaxList(decls));
    public static CompilationUnitSyntax CompilationUnit(params DeclarationSyntax[] decls) =>
        CompilationUnit(SyntaxList(decls));

    public static ModuleDeclarationSyntax ModuleDeclaration(string name, IEnumerable<DeclarationSyntax> declarations) =>
        ModuleDeclaration(SyntaxList<AttributeSyntax>(), null, name, SyntaxList(declarations));
    public static ModuleDeclarationSyntax ModuleDeclaration(string name, params DeclarationSyntax[] declarations) =>
        ModuleDeclaration(name, declarations.AsEnumerable());

    public static ImportDeclarationSyntax ImportDeclaration(string root, params string[] path) => ImportDeclaration(
        SyntaxList<AttributeSyntax>(),
        null,
        path.Aggregate(
            RootImportPath(Identifier(root)) as ImportPathSyntax,
            (path, member) => MemberImportPath(path, Dot, Identifier(member))));

    public static FunctionDeclarationSyntax FunctionDeclaration(
        string name,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            [],
            Visibility.Private,
            name,
            null,
            parameters,
            returnType,
            body);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        IEnumerable<AttributeSyntax> attributes,
        string name,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            attributes,
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
            [],
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
            [],
            Visibility.Private,
            name,
            generics,
            parameters,
            returnType,
            body);

    public static FunctionDeclarationSyntax FunctionDeclaration(
        IEnumerable<AttributeSyntax> attributes,
        Visibility visibility,
        string name,
        SeparatedSyntaxList<GenericParameterSyntax>? generics,
        SeparatedSyntaxList<ParameterSyntax> parameters,
        TypeSyntax? returnType,
        FunctionBodySyntax body) => FunctionDeclaration(
            SyntaxList(attributes),
            VisibilityToken(visibility),
            name,
            generics is null ? null : GenericParameterList(generics),
            parameters,
            returnType is null ? null : TypeSpecifier(returnType),
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
        SyntaxList<AttributeSyntax>(),
        visibility,
        isMutable ? KeywordVar : KeywordVal,
        name,
        type is null ? null : TypeSpecifier(type),
        value is null ? null : ValueSpecifier(value));

    public static LabelDeclarationSyntax LabelDeclaration(string name) =>
        LabelDeclaration(SyntaxList<AttributeSyntax>(), null, name);

    public static AttributeSyntax Attribute(TypeSyntax type, params ExpressionSyntax[] args) =>
        Attribute(type, args.AsEnumerable());

    public static AttributeSyntax Attribute(TypeSyntax type, IEnumerable<ExpressionSyntax> args) =>
        Attribute(type, ArgumentList(SeparatedSyntaxList(Comma, args)));

    public static BlockFunctionBodySyntax BlockFunctionBody(IEnumerable<StatementSyntax> stmts) => BlockFunctionBody(SyntaxList(stmts));
    public static BlockFunctionBodySyntax BlockFunctionBody(params StatementSyntax[] stmts) => BlockFunctionBody(stmts.AsEnumerable());

    public static BlockExpressionSyntax BlockExpression(
        IEnumerable<StatementSyntax> stmts,
        ExpressionSyntax? value = null) => BlockExpression(
        SyntaxList(stmts),
        value);
    public static BlockExpressionSyntax BlockExpression(params StatementSyntax[] stmts) => BlockExpression(stmts.AsEnumerable());

    public static IfExpressionSyntax IfExpression(
        ExpressionSyntax condition,
        ExpressionSyntax then,
        ExpressionSyntax? @else) => IfExpression(
        condition,
        then,
        @else is null ? null : ElseClause(@else));

    public static ForExpressionSyntax ForExpression(
        string iterator,
        ExpressionSyntax sequence,
        ExpressionSyntax body) => ForExpression(
        iterator,
        null,
        sequence,
        body);

    public static CallExpressionSyntax CallExpression(
        ExpressionSyntax called,
        IEnumerable<ExpressionSyntax> args) => CallExpression(
        called,
        SeparatedSyntaxList(Comma, args));
    public static CallExpressionSyntax CallExpression(
        ExpressionSyntax called,
        params ExpressionSyntax[] args) => CallExpression(called, args.AsEnumerable());

    public static GenericExpressionSyntax GenericExpression(
        ExpressionSyntax instantiated,
        params TypeSyntax[] typeParameters) => GenericExpression(instantiated, SeparatedSyntaxList(Comma, typeParameters));
    public static GenericTypeSyntax GenericType(TypeSyntax instantiated, params TypeSyntax[] typeParameters) =>
        GenericType(instantiated, SeparatedSyntaxList(Comma, typeParameters));

    public static IndexExpressionSyntax IndexExpression(ExpressionSyntax indexed, params ExpressionSyntax[] indices) =>
        IndexExpression(indexed, SeparatedSyntaxList(Comma, indices));

    public static GotoExpressionSyntax GotoExpression(string label) => GotoExpression(NameLabel(Identifier(label)));

    public static LiteralExpressionSyntax LiteralExpression(int value) => LiteralExpression(Integer(value));
    public static LiteralExpressionSyntax LiteralExpression(bool value) => LiteralExpression(value ? KeywordTrue : KeywordFalse);
    public static StringExpressionSyntax StringExpression(string value) =>
        StringExpression(LineStringStart, SyntaxList(TextStringPart(value) as StringPartSyntax), LineStringEnd);

    public static TextStringPartSyntax TextStringPart(string value) =>
        TextStringPart(Token(TokenKind.StringContent, value, value));

    // TOKENS //////////////////////////////////////////////////////////////////

    public static SyntaxToken LineStringStart { get; } = Token(TokenKind.LineStringStart, "\"");
    public static SyntaxToken LineStringEnd { get; } = Token(TokenKind.LineStringEnd, "\"");

    private static SyntaxToken Token(TokenKind tokenKind) =>
        Internal.Syntax.SyntaxToken.From(tokenKind).ToRedNode(null!, null, 0);
    private static SyntaxToken Token(TokenKind tokenKind, string text) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, text).ToRedNode(null!, null, 0);
    private static SyntaxToken Token(TokenKind tokenKind, string text, object? value) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, text, value).ToRedNode(null!, null, 0);
    private static SyntaxToken Token(TokenKind tokenKind, object? value) =>
        Internal.Syntax.SyntaxToken.From(tokenKind, value: value).ToRedNode(null!, null, 0);
}
