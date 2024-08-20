using System.Collections.Generic;
using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Provides syntax highlighting for source code.
/// </summary>
public static class SyntaxHighlighter
{
    /// <summary>
    /// Syntax highlights the given <paramref name="tree"/>, optionally using the given <paramref name="semanticModel"/>.
    /// </summary>
    /// <param name="tree">The syntax tree to highlight.</param>
    /// <param name="semanticModel">The semantic model to use for highlighting for more accurate results.</param>
    /// <returns>The highlighted fragments of the source code.</returns>
    public static IEnumerable<HighlightFragment> Highlight(SyntaxTree tree, SemanticModel? semanticModel = null)
    {
        foreach (var token in tree.Root.Tokens)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                foreach (var fragment in Highlight(trivia, semanticModel)) yield return fragment;
            }

            foreach (var fragment in Highlight(token, semanticModel)) yield return fragment;

            foreach (var trivia in token.TrailingTrivia)
            {
                foreach (var fragment in Highlight(trivia, semanticModel)) yield return fragment;
            }
        }
    }

    private static IEnumerable<HighlightFragment> Highlight(SyntaxTrivia trivia, SemanticModel? semanticModel) => trivia.Kind switch
    {
        _ => Fragment(trivia, SyntaxColoring.Unknown),
    };

    private static IEnumerable<HighlightFragment> Highlight(SyntaxToken token, SemanticModel? semanticModel) => token.Kind switch
    {
        _ => Fragment(token, SyntaxColoring.Unknown),
    };

    private static IEnumerable<HighlightFragment> Fragment(SyntaxTrivia trivia, SyntaxColoring color) =>
        [new HighlightFragment(trivia.Range, trivia.Text, color)];

    private static IEnumerable<HighlightFragment> Fragment(SyntaxToken token, SyntaxColoring color) =>
        [new HighlightFragment(token.Range, token.Text, color)];
}
