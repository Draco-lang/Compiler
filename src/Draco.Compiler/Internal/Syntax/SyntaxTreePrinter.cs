using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Utilities for printing a <see cref="SyntaxTree"/>.
/// </summary>
internal static class SyntaxTreePrinter
{
    /// <summary>
    /// Prints the <see cref="SyntaxNode"/> as the text it was parsed from.
    /// </summary>
    /// <param name="node">The tree node to print.</param>
    /// <returns>The <paramref name="node"/> printed to text, identical to the text it was parsed from.</returns>
    public static string ToCode(SyntaxNode node)
    {
        var result = new StringBuilder();
        foreach (var token in node.Tokens)
        {
            foreach (var t in token.LeadingTrivia) result.Append(t.Text);
            result.Append(token.Text);
            foreach (var t in token.TrailingTrivia) result.Append(t.Text);
        }
        return result.ToString();
    }

    /// <summary>
    /// Prints the <see cref="ParseNode"/> as the text it was parsed from, discarding the very first leading trivia
    /// and the very last trailing trivia, "trimming" the code.
    /// </summary>
    /// <param name="node">The tree node to print.</param>
    /// <returns>The <paramref name="node"/> printed to text, without the surrounding trivia.</returns>
    public static string ToCodeWithoutSurroundingTrivia(SyntaxNode node)
    {
        var result = new StringBuilder();
        // We simply print the text of all tokens except the first and last ones
        // For the first, we ignore leading trivia, for the last we ignore trailing trivia
        var lastTrailingTrivia = SyntaxList<SyntaxTrivia>.Empty;
        using var tokenEnumerator = node.Tokens.GetEnumerator();
        // The first token just gets it's content printed
        // That ignores the leading trivia, trailing will only be printed if there are following tokens
        var hasFirstToken = tokenEnumerator.MoveNext();
        if (!hasFirstToken) return string.Empty;
        var firstToken = tokenEnumerator.Current;
        result.Append(firstToken.Text);
        lastTrailingTrivia = firstToken.TrailingTrivia;
        while (tokenEnumerator.MoveNext())
        {
            var token = tokenEnumerator.Current;
            // Last trailing trivia
            foreach (var t in lastTrailingTrivia) result.Append(t.Text);
            // Leading trivia
            foreach (var t in token.LeadingTrivia) result.Append(t.Text);
            // Content
            result.Append(token.Text);
            // Trailing trivia
            lastTrailingTrivia = token.TrailingTrivia;
        }
        return result.ToString();
    }

    /// <summary>
    /// Prints the given subtree as a DOT graph.
    /// </summary>
    /// <param name="node">The root of the subtree to print.</param>
    /// <returns>The <paramref name="node"/> represented as a DOT graph.</returns>
    public static string ToDot(SyntaxNode node)
    {
        var graph = new DotGraphBuilder<SyntaxNode>(isDirected: false, vertexComparer: ReferenceEqualityComparer.Instance);
        graph.WithName("ParseTree");

        void Impl(SyntaxNode? parent, SyntaxNode node)
        {
            // Connect to parent
            if (parent is not null) graph.AddEdge(node, parent);
            // Label
            graph!
                .AddVertex(node)
                .WithLabel(node is SyntaxToken t ? t.Text : node.GetType().Name);
            // Children
            foreach (var child in node.Children) Impl(node, child);
        }

        Impl(null, node);
        return graph.ToDot();
    }
}
