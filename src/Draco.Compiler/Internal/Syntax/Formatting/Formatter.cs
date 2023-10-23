using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// A formatter for the syntax tree.
/// </summary>
internal sealed class Formatter : SyntaxRewriter
{
    private static readonly object Space = new();
    private static readonly object Newline = new();
    private static readonly object Newline2 = new();
    private static readonly object Indent = new();
    private static readonly object Unindent = new();

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private SyntaxTrivia? LastTrivia
    {
        get
        {
            if (this.currentTrivia.Count > 0) return this.currentTrivia[^1];
            if (this.lastToken is null) return null;
            if (this.lastToken.TrailingTrivia.Count == 0) return null;
            return this.lastToken.TrailingTrivia[^1];
        }
    }

    private int indentation;
    private SyntaxToken? lastToken;
    private SyntaxList<SyntaxTrivia>.Builder currentTrivia = new();

    public Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
    }

    public override SyntaxNode VisitCompilationUnit(CompilationUnitSyntax node) => node.Update(this.AppendSequence(
        node.Declarations,
        Newline,
        node.End));

    public override SyntaxNode VisitFunctionDeclaration(FunctionDeclarationSyntax node) => node.Update(this.AppendSequence(
        node.VisibilityModifier,
        Space,
        node.FunctionKeyword,
        Space,
        node.Name,
        node.Generics,
        node.OpenParen,
        node.ParameterList,
        node.CloseParen,
        node.ReturnType,
        Space,
        node.Body));

    public override SyntaxNode VisitBlockFunctionBody(BlockFunctionBodySyntax node) => node.Update(this.AppendSequence(
        node.OpenBrace,
        Newline,
        Indent,
        node.Statements,
        Unindent,
        node.CloseBrace));

    public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node) => node.Update(this.AppendSequence(
        node.VisibilityModifier,
        Space,
        node.Keyword,
        Space,
        node.Name,
        node.Type,
        node.Value,
        node.Semicolon,
        Newline));

    public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node) => node.Update(this.AppendSequence(
        node.Expression,
        node.Semicolon,
        Newline));

    public override SyntaxNode VisitIfExpression(IfExpressionSyntax node) => node.Update(this.AppendSequence(
        node.IfKeyword,
        Space,
        node.OpenParen,
        node.Condition,
        node.CloseParen,
        Space,
        node.Then,
        node.Else));

    public override SyntaxNode VisitElseClause(ElseClauseSyntax node) => node.Update(this.AppendSequence(
        Space,
        node.ElseKeyword,
        Space,
        node.Expression));

    public override SyntaxNode VisitWhileExpression(WhileExpressionSyntax node) => node.Update(this.AppendSequence(
        node.WhileKeyword,
        Space,
        node.OpenParen,
        node.Condition,
        node.CloseParen,
        Space,
        node.Then));

    public override SyntaxNode VisitBlockExpression(BlockExpressionSyntax node) => node.Update(this.AppendSequence(
        node.OpenBrace,
        Newline,
        Indent,
        node.Statements,
        node.Value,
        Unindent,
        node.CloseBrace));

    public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node) => node.Update(this.AppendSequence(
        node.Left,
        Space,
        node.Operator,
        Space,
        node.Right));

    public override SyntaxNode VisitGroupingExpression(GroupingExpressionSyntax node) => node.Update(this.AppendSequence(
        node.OpenParen,
        node.Expression,
        node.CloseParen));

    public override SyntaxNode VisitTypeSpecifier(TypeSpecifierSyntax node) => node.Update(this.AppendSequence(
        node.Colon,
        Space,
        node.Type));

    public override SyntaxNode VisitValueSpecifier(ValueSpecifierSyntax node) => node.Update(this.AppendSequence(
        Space,
        node.Assign,
        Space,
        node.Value));

    public override SyntaxNode VisitSyntaxToken(SyntaxToken node)
    {
        this.AddNormalizedLeadingTrivia(node.LeadingTrivia);
        this.EnsureIndentation(this.indentation);

        var leadingTrivia = this.currentTrivia.ToSyntaxList();
        this.currentTrivia.Clear();

        this.AddNormalizedTrailingTrivia(node.TrailingTrivia);
        var trailingTrivia = this.currentTrivia.ToSyntaxList();
        this.currentTrivia.Clear();

        // TODO: Not too efficient, we copy twice...
        var newTokenBuilder = SyntaxToken.Builder.From(node);
        newTokenBuilder.LeadingTrivia = leadingTrivia.ToBuilder();
        newTokenBuilder.TrailingTrivia = trailingTrivia.ToBuilder();
        var newToken = newTokenBuilder.Build();

        this.lastToken = newToken;

        return newToken;
    }

    private IEnumerable<SyntaxNode?> AppendSequence(params object?[] elements) =>
        this.AppendSequence(elements.AsEnumerable());

    private IEnumerable<SyntaxNode?> AppendSequence(IEnumerable<object?> elements)
    {
        foreach (var element in elements)
        {
            if (element is null)
            {
                yield return null;
            }
            else if (ReferenceEquals(element, Indent))
            {
                ++this.indentation;
            }
            else if (ReferenceEquals(element, Unindent))
            {
                --this.indentation;
            }
            else if (ReferenceEquals(element, Space))
            {
                this.EnsureSpace();
            }
            else if (ReferenceEquals(element, Newline))
            {
                this.EnsureNewlines(1);
            }
            else if (ReferenceEquals(element, Newline2))
            {
                this.EnsureNewlines(2);
            }
            else if (element is SyntaxNode node)
            {
                yield return node.Accept(this);
            }
            else
            {
                throw new ArgumentException($"can not handle sequence element {element}");
            }
        }
    }

    private void AddNormalizedLeadingTrivia(SyntaxList<SyntaxTrivia> trivia)
    {
        // We only add comments and newlines after that
        foreach (var t in trivia)
        {
            if (t.Kind is not TriviaKind.LineComment or TriviaKind.DocumentationComment) continue;

            // Indent the trivia
            this.EnsureIndentation(this.indentation);
            // Add comment
            this.currentTrivia.Add(t);
            // Add a newline after
            this.currentTrivia.Add(this.Settings.NewlineTrivia);
        }
    }

    private void AddNormalizedTrailingTrivia(SyntaxList<SyntaxTrivia> trivia)
    {
        // We only add comments and newlines after that
        var first = true;
        foreach (var t in trivia)
        {
            if (t.Kind is not TriviaKind.LineComment or TriviaKind.DocumentationComment) continue;

            // Indent the trivia
            if (first) this.EnsureSpace();
            else this.EnsureIndentation(this.indentation);
            // Add comment
            this.currentTrivia.Add(t);
            // Add a newline after
            this.currentTrivia.Add(this.Settings.NewlineTrivia);
            first = false;
        }
    }

    private void EnsureNewlines(int amount)
    {
        var existingNewlines = 0;

        // Count how many newlines in current trivia
        var allNewline = true;
        for (var i = this.currentTrivia.Count - 1; i >= 0; --i)
        {
            var trivia = this.currentTrivia[i];
            if (trivia.Kind == TriviaKind.Newline)
            {
                ++existingNewlines;
            }
            else
            {
                allNewline = false;
                break;
            }
        }

        // If it was all newlines, add the last tokens trailing trivia newlines
        if (allNewline && this.lastToken is not null)
        {
            var trailingTrivia = this.lastToken.TrailingTrivia;
            for (var i = trailingTrivia.Count - 1; i >= 0; --i)
            {
                var trivia = trailingTrivia[i];
                if (trivia.Kind == TriviaKind.Newline)
                {
                    ++existingNewlines;
                }
                else
                {
                    break;
                }
            }
        }

        // Add newlines if needed
        for (var i = existingNewlines; i < amount; ++i)
        {
            this.currentTrivia.Add(this.Settings.NewlineTrivia);
        }
    }

    private void EnsureSpace()
    {
        // Assume no need to indent first token
        if (this.lastToken is null) return;

        var lastTrivia = this.LastTrivia;

        // Done, equivalent
        if (lastTrivia is not null && lastTrivia.Kind is TriviaKind.Whitespace or TriviaKind.Newline) return;

        // Need a space
        this.currentTrivia.Add(this.Settings.SpaceTrivia);
    }

    private void EnsureIndentation(int indentation)
    {
        // Assume no need to indent first token
        if (this.lastToken is null) return;

        // Check prev trivia
        var lastTrivia = this.LastTrivia;
        if (lastTrivia is null || lastTrivia.Kind != TriviaKind.Newline) return;

        // Not indented, last trivia was a newline
        this.currentTrivia.Add(this.Settings.IndentationTrivia(indentation));
    }
}
