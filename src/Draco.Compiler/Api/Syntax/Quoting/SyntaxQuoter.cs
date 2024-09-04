using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Api.Syntax.Quoting;

/// <summary>
/// Specifies the output language for the quoter.
/// </summary>
public enum OutputLanguage
{
    /// <summary>
    /// Output as C# code.
    /// </summary>
    CSharp

    // Probably going to add an option for Draco at some point
}

/// <summary>
/// Specifies what the quoter should
/// </summary>
public enum QuoteMode
{
    /// <summary>
    /// Quote an entire file.
    /// </summary>
    File,

    /// <summary>
    /// Quote a single declaration.
    /// </summary>
    Declaration,

    /// <summary>
    /// Quote a single statement.
    /// </summary>
    Statement,

    /// <summary>
    /// Quote a single expression.
    /// </summary>
    Expression
}

/// <summary>
/// Produces quoted text from <see cref="SourceText"/>s or <see cref="SyntaxNode"/>s.
/// </summary>
/// <param name="outputLanguage">The target output language for the quoter.</param>
/// <param name="quoteMode">The mode the quoter should operate in.</param>
public sealed partial class SyntaxQuoter(OutputLanguage outputLanguage)
{
    /// <summary>
    /// Produces a string containing the factory method calls required to produce a string of source code.
    /// </summary>
    /// <param name="text">The <see cref="SourceText"/> to quote.</param>
    /// <returns>A string containing the quoted text.</returns>
    public string Quote(SourceText text, QuoteMode mode)
    {
        // Todo: probably factor out this duplicate code
        var diags = new SyntaxDiagnosticTable();
        var srcReader = text.SourceReader;
        var lexer = new Lexer(srcReader, diags);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource, diags);

        Internal.Syntax.SyntaxNode node = mode switch
        {
            QuoteMode.File => parser.ParseCompilationUnit(),
            QuoteMode.Declaration => parser.ParseDeclaration(),
            QuoteMode.Statement => parser.ParseStatement(false), // Todo: allow declarations?
            QuoteMode.Expression => parser.ParseExpression(),
            _ => throw new ArgumentOutOfRangeException(nameof(mode))
        };

        var red = node.ToRedNode(null!, null, 0);

        return this.Quote(red);
    }

    /// <summary>
    /// Produces a string containing the factory method calls required to produce a syntax node.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to quote.</param>
    /// <returns>A string containing the quoted text.</returns>
    public string Quote(SyntaxNode node)
    {
        var visitor = new QuoteVisitor();
        var expr = node.Accept(visitor);
        throw new NotImplementedException();
    }

    private static QuoteExpression QuoteObjectLiteral(object? value) => value switch
    {
        null => new QuoteNull(),
        int @int => new QuoteInteger(@int),
        float @float => new QuoteFloat(@float),
        string @string => new QuoteString(@string),
        bool @bool => new QuoteBoolean(@bool),
        // Todo: are there any more literal values tokens can have?
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private sealed partial class QuoteVisitor : SyntaxVisitor<QuoteExpression>
    {
        public override QuoteExpression VisitSyntaxToken(SyntaxToken node)
        {
            var kindQuote = new QuoteTokenKind(node.Kind);
            return (SyntaxFacts.GetTokenText(node.Kind), node.Value) switch
            {
                (not null, null) => new QuoteFunctionCall("MakeToken", [kindQuote]),
                (null, null) => new QuoteFunctionCall("MakeToken", [
                    kindQuote,
                    new QuoteString(StringUtils.Unescape(node.Text))
                ]),
                (not null, not null) => new QuoteFunctionCall("MakeToken", [
                    kindQuote,
                    QuoteObjectLiteral(node.Value)
                ]),
                (null, not null) => new QuoteFunctionCall("MakeToken", [
                    kindQuote,
                    new QuoteString(StringUtils.Unescape(node.Text)),
                    QuoteObjectLiteral(node.Value)
                ])
            };
        }

        public override QuoteExpression VisitSyntaxTrivia(SyntaxTrivia node) =>
            throw new NotSupportedException("Quoter does currently not support quoting syntax trivia.");

        public override QuoteExpression VisitSyntaxList<TNode>(SyntaxList<TNode> node) =>
            new QuoteFunctionCall(
                "SyntaxList",
                [typeof(TNode).FullName!], // Todo: hack
                [new QuoteList(node.Select(n => n.Accept(this)).ToImmutableArray())]);

        public override QuoteExpression VisitSeparatedSyntaxList<TNode>(SeparatedSyntaxList<TNode> node) =>
            new QuoteFunctionCall(
                "SeparatedSyntaxList",
                [typeof(TNode).FullName!], // Todo: hack
                [
                new QuoteList(node.Separators.Select(x => x.Accept(this)).ToImmutableArray()),
                new QuoteList(node.Values.Select(x => x.Accept(this)).ToImmutableArray())]);
    }
}
