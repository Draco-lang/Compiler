using Draco.Compiler.Internal.Syntax.Formatting;
using Draco.Compiler.Internal.Syntax.Rewriting;
using System.Collections.Immutable;

namespace Draco.Compiler.Api.Syntax.Extensions;

/// <summary>
/// Extensions for rewriting syntax trees.
/// </summary>
public static class SyntaxTreeRewriteExtensions
{
    /// <summary>
    /// Inserts a node before another node in the syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to rewrite.</param>
    /// <param name="toInsert">The node to insert.</param>
    /// <param name="insertBefore">The node to insert before.</param>
    /// <returns>The rewritten syntax tree.</returns>
    public static SyntaxTree InsertBefore(this SyntaxTree tree, SyntaxNode toInsert, SyntaxNode insertBefore) => new(
        tree.SourceText,
        InsertRewriter.InsertBefore(tree.GreenRoot, toInsert.Green, insertBefore.Green),
        new());

    /// <summary>
    /// Inserts a node after another node in the syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to rewrite.</param>
    /// <param name="toInsert">The node to insert.</param>
    /// <param name="insertBefore">The node to insert after.</param>
    /// <returns>The rewritten syntax tree.</returns>
    public static SyntaxTree InsertAfter(this SyntaxTree tree, SyntaxNode toInsert, SyntaxNode insertBefore) => new(
        tree.SourceText,
        InsertRewriter.InsertAfter(tree.GreenRoot, toInsert.Green, insertBefore.Green),
        new());

    /// <summary>
    /// Removes a node from the syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to rewrite.</param>
    /// <param name="toRemove">The node to remove.</param>
    /// <returns>The rewritten syntax tree.</returns>
    public static SyntaxTree Remove(this SyntaxTree tree, SyntaxNode toRemove) => new(
        tree.SourceText,
        RemoveRewriter.Remove(tree.GreenRoot, toRemove.Green),
        new());

    /// <summary>
    /// Calculates the edits needed to transform one syntax tree into another.
    /// </summary>
    /// <param name="tree">The original syntax tree.</param>
    /// <param name="target">The target syntax tree.</param>
    /// <returns>The edits needed to transform the original syntax tree into the target syntax tree.</returns>
    public static ImmutableArray<TextEdit> CalculateEdits(this SyntaxTree tree, SyntaxTree target) =>
        // TODO: We can use a better diff algo
        [new TextEdit(tree.Root.Span, target.ToString())];

    /// <summary>
    /// Formats the syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <returns>The formatted syntax tree.</returns>
    public static SyntaxTree Format(this SyntaxTree tree) => Formatter.Format(tree);
}
