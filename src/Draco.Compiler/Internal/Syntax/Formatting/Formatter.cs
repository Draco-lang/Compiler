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

    public override void VisitSyntaxToken(SyntaxToken node)
    {
        // TODO
        this.tokens.Add(node.ToBuilder());
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
        for (var i = 0; i < this.tokens.Count; ++i)
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
