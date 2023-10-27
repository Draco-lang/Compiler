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
internal sealed class Formatter : SyntaxVisitor
{
    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static SyntaxTree Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;
        var formatter = new Formatter(settings);

        // Construct token sequence
        tree.GreenRoot.Accept(formatter);

        // Re-parse into tree
        var tokens = formatter.tokens
            .Select(t => t.Build())
            .ToArray();
        var tokenSource = TokenSource.From(tokens.AsMemory());
        // TODO: Pass in anything for diagnostics?
        var parser = new Parser(tokenSource, diagnostics: new());
        // TODO: Is it correct to assume compilation unit?
        var formattedRoot = parser.ParseCompilationUnit();

        return new SyntaxTree(
            // TODO: Is this correct to pass it in?
            sourceText: tree.SourceText,
            greenRoot: formattedRoot,
            // TODO: Anything smarter to pass in?
            syntaxDiagnostics: new());
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private readonly List<SyntaxToken.Builder> tokens = new();
    private readonly SyntaxList<SyntaxTrivia>.Builder currentTrivia = new();
    private int indentation;

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
    }

    public override void VisitCompilationUnit(CompilationUnitSyntax node)
    {
        this.FormatWithImports(node.Declarations);
        // NOTE: Is is safe to clear this?
        this.currentTrivia.Clear();
        this.Newline();
        this.Place(node.End);
    }

    public override void VisitDeclarationStatement(DeclarationStatementSyntax node)
    {
        this.Place(node.Declaration);
        this.Newline();
    }

    public override void VisitImportDeclaration(ImportDeclarationSyntax node)
    {
        this.Place(node.ImportKeyword);
        this.Space();
        this.Place(node.Path);
        this.Place(node.Semicolon);
        this.Newline();
    }

    public override void VisitLabelDeclaration(LabelDeclarationSyntax node)
    {
        this.Unindent();
        this.Place(node.Name);
        this.Place(node.Colon);
        this.Indent();
    }

    public override void VisitFunctionDeclaration(FunctionDeclarationSyntax node)
    {
        this.Place(node.FunctionKeyword);
        this.Space();
        this.Place(node.Name);
        this.Place(node.OpenParen);
        this.AfterSeparator(node.ParameterList, this.Space);
        this.Place(node.CloseParen);
        this.Place(node.ReturnType);
        this.Space();
        this.Place(node.Body);
    }

    public override void VisitBlockFunctionBody(BlockFunctionBodySyntax node)
    {
        this.Place(node.OpenBrace);
        if (node.Statements.Count > 0) this.Newline();
        this.Indent();
        this.FormatWithImports(node.Statements);
        this.Unindent();
        this.Place(node.CloseBrace);
        this.Newline(2);
    }

    public override void VisitInlineFunctionBody(InlineFunctionBodySyntax node)
    {
        this.Place(node.Assign);
        this.Space();
        this.Indent();
        this.Place(node.Value);
        this.Place(node.Semicolon);
        this.Unindent();
    }

    public override void VisitExpressionStatement(ExpressionStatementSyntax node)
    {
        this.Place(node.Expression);
        this.Place(node.Semicolon);
        this.Newline();
    }

    public override void VisitReturnExpression(ReturnExpressionSyntax node)
    {
        this.Place(node.ReturnKeyword);
        this.SpaceBeforeNotNull(node.Value);
    }

    public override void VisitGotoExpression(GotoExpressionSyntax node)
    {
        this.Place(node.GotoKeyword);
        this.Space();
        this.Place(node.Target);
    }

    public override void VisitTypeSpecifier(TypeSpecifierSyntax node)
    {
        this.Place(node.Colon);
        this.Space();
        this.Place(node.Type);
    }

    // Formatting a list with potential import declarations within
    private void FormatWithImports<T>(SyntaxList<T> list)
        where T : SyntaxNode
    {
        var lastWasImport = false;
        foreach (var item in list)
        {
            if (item is ImportDeclarationSyntax)
            {
                if (!lastWasImport) this.Newline(2);
                lastWasImport = true;
            }
            else
            {
                if (lastWasImport) this.Newline(2);
                lastWasImport = false;
            }
            this.Place(item);
        }
    }

    // ELemental token formatting
    public override void VisitSyntaxToken(SyntaxToken node)
    {
        var builder = node.ToBuilder();

        // Normalize trivia
        this.NormalizeLeadingTrivia(builder.LeadingTrivia, this.indentation);
        this.NormalizeTrailingTrivia(builder.TrailingTrivia, this.indentation);

        // Add what is accumulated
        builder.LeadingTrivia.InsertRange(0, this.currentTrivia);

        // Indent
        if (this.tokens.Count > 0)
        {
            this.EnsureIndentation(this.tokens[^1].TrailingTrivia, builder.LeadingTrivia, this.indentation);
        }

        // Clear state
        this.currentTrivia.Clear();

        // Append
        this.tokens.Add(builder);
    }

    // Format actions //////////////////////////////////////////////////////////

    private void Place(SyntaxNode? node)
    {
        if (node is null) return;
        node.Accept(this);
    }
    private void Indent() => ++this.indentation;
    private void Unindent() => --this.indentation;
    private void Space()
    {
        if (this.tokens.Count == 0) return;
        this.EnsureSpace(this.tokens[^1].TrailingTrivia, this.currentTrivia);
    }
    private void Newline(int amount = 1)
    {
        if (this.tokens.Count == 0) return;
        this.EnsureNewline(this.tokens[^1].TrailingTrivia, this.currentTrivia, amount);
    }
    private void SpaceBeforeNotNull(SyntaxNode? node)
    {
        if (node is null) return;
        this.Space();
        this.Place(node);
    }
    private void AfterSeparator<T>(SeparatedSyntaxList<T> list, Action afterSep)
        where T : SyntaxNode
    {
        var isSeparator = false;
        foreach (var item in list)
        {
            this.Place(item);
            if (isSeparator) afterSep();
            isSeparator = !isSeparator;
        }
    }

    // Low level utilities /////////////////////////////////////////////////////

    private void NormalizeLeadingTrivia(
        SyntaxList<SyntaxTrivia>.Builder trivia,
        int indentation)
    {
        static bool IsSpace(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Newline or TriviaKind.Whitespace;

        static bool IsComment(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.LineComment or TriviaKind.DocumentationComment;

        // Remove all space
        for (var i = 0; i < trivia.Count;)
        {
            if (IsSpace(trivia[i])) trivia.RemoveAt(i);
            else ++i;
        }

        // Indent the trivia if needed
        if (this.tokens.Count > 0)
        {
            this.EnsureIndentation(this.tokens[^1].TrailingTrivia, trivia, indentation);
        }

        // Before each comment or doc comment, we add a newline, then indentation
        // Except the first one, which just got indented
        var isFirst = true;
        for (var i = 0; i < trivia.Count; ++i)
        {
            if (!IsComment(trivia[i])) continue;
            if (isFirst)
            {
                isFirst = false;
                continue;
            }
            // A comment comes next, add newline then indentation
            trivia.Insert(i, this.Settings.NewlineTrivia);
            if (indentation > 0) trivia.Insert(i + 1, this.Settings.IndentationTrivia(indentation));
        }
    }

    private void NormalizeTrailingTrivia(
        SyntaxList<SyntaxTrivia>.Builder trivia,
        int indentation)
    {
        static bool IsSpace(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Newline or TriviaKind.Whitespace;

        // Remove all space
        for (var i = 0; i < trivia.Count;)
        {
            if (IsSpace(trivia[i])) trivia.RemoveAt(i);
            else ++i;
        }

        // If nonempty, add a space and a newline at the end
        if (trivia.Count > 0)
        {
            trivia.Insert(0, this.Settings.SpaceTrivia);
            trivia.Add(this.Settings.NewlineTrivia);
        }
    }

    private void EnsureIndentation(
        SyntaxList<SyntaxTrivia>.Builder first,
        SyntaxList<SyntaxTrivia>.Builder second,
        int indentation)
    {
        // The first didn't end in a newline, no need to indent
        if (first.Count == 0) return;
        if (first[^1].Kind != TriviaKind.Newline) return;

        // Trim the second one
        TrimLeft(second, TriviaKind.Whitespace);

        // Add the indentation, if it's > 0
        if (indentation > 0) second.Insert(0, this.Settings.IndentationTrivia(indentation));
    }

    private void EnsureSpace(
        SyntaxList<SyntaxTrivia>.Builder first,
        SyntaxList<SyntaxTrivia>.Builder second)
    {
        static bool IsSpace(SyntaxTrivia trivia) =>
            trivia.Kind is TriviaKind.Newline or TriviaKind.Whitespace;

        if (first.Count > 0 && IsSpace(first[^1])) return;
        if (second.Count > 0 && IsSpace(second[0])) return;

        // We can just append at the end of the first
        first.Add(this.Settings.SpaceTrivia);
    }

    private void EnsureNewline(
        SyntaxList<SyntaxTrivia>.Builder first,
        SyntaxList<SyntaxTrivia>.Builder second,
        int amount)
    {
        // Count existing
        var firstNewlines = 0;
        for (var i = first.Count - 1; i >= 0; --i)
        {
            if (first[i].Kind != TriviaKind.Newline) break;
            ++firstNewlines;
        }
        var secondNewlines = 0;
        for (var i = 0; i < second.Count; ++i)
        {
            if (second[i].Kind != TriviaKind.Newline) break;
            ++secondNewlines;
        }

        // Append any that's needed
        var missing = amount - (firstNewlines + secondNewlines);
        for (var i = 0; i < missing; ++i)
        {
            if (i == 0 && firstNewlines == 0)
            {
                // The first didn't end in a newline, its trailing trivia can end in a newline
                // Add the first one there
                first.Add(this.Settings.NewlineTrivia);
            }
            else
            {
                // Add to second
                second.Insert(0, this.Settings.NewlineTrivia);
            }
        }
    }

    private static void TrimLeft(SyntaxList<SyntaxTrivia>.Builder builder, params TriviaKind[] toTrim)
    {
        var n = 0;
        while (builder.Count > n && toTrim.Contains(builder[n].Kind)) ++n;
        builder.RemoveRange(0, n);
    }

    private static void TrimRight(SyntaxList<SyntaxTrivia>.Builder builder, params TriviaKind[] toTrim)
    {
        var n = 0;
        while (builder.Count > n && toTrim.Contains(builder[builder.Count - n - 1].Kind)) ++n;
        builder.RemoveRange(builder.Count - n - 1, n);
    }
}
