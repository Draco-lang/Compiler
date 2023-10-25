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

        var formattedRoot = tree.GreenRoot.Accept(formatter);

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

    private Formatter(FormatterSettings settings)
    {
        this.Settings = settings;
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
