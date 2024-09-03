using System;
using Draco.Compiler.Internal.Syntax;

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

        return this.Quote(node);
    }

    /// <summary>
    /// Produces a string containing the factory method calls required to produce a syntax node.
    /// </summary>
    /// <param name="node">The <see cref="Api.Syntax.SyntaxNode"/> to quote.</param>
    /// <returns>A string containing the quoted text.</returns>
    public string Quote(Api.Syntax.SyntaxNode node) =>
        this.Quote(node.Green);

    private string Quote(Internal.Syntax.SyntaxNode node)
    {
        var visitor = new QuoteVisitor();
        var expr = node.Accept(visitor);
        throw new NotImplementedException();
    }

    private sealed partial class QuoteVisitor : Internal.Syntax.SyntaxVisitor<QuoteExpression>
    {
        public override QuoteExpression VisitSyntaxToken(Internal.Syntax.SyntaxToken node)
        {
            throw new NotImplementedException();
        }

        public override QuoteExpression VisitSyntaxTrivia(Internal.Syntax.SyntaxTrivia node)
        {
            throw new NotImplementedException();
        }

        public override QuoteExpression VisitSyntaxList<TNode>(Internal.Syntax.SyntaxList<TNode> node)
        {
            throw new NotImplementedException();
        }

        public override QuoteExpression VisitSeparatedSyntaxList<TNode>(Internal.Syntax.SeparatedSyntaxList<TNode> node)
        {
            throw new NotImplementedException();
        }
    }
}
