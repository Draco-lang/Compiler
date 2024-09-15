using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

/// <summary>
/// A syntax rewriter that removes a node from the syntax tree.
/// </summary>
internal sealed class RemoveRewriter : SyntaxRewriter
{
    /// <summary>
    /// Removes a node from the syntax tree.
    /// </summary>
    /// <param name="root">The root node to rewrite.</param>
    /// <param name="toRemove">The node to remove.</param>
    /// <returns>The rewritten root node.</returns>
    public static SyntaxNode Remove(SyntaxNode root, SyntaxNode toRemove) =>
        root.Accept(new RemoveRewriter(toRemove));

    private readonly SyntaxNode toRemove;

    private RemoveRewriter(SyntaxNode toRemove)
    {
        this.toRemove = toRemove;
    }

    protected override ImmutableArray<TNode>? RewriteArray<TNode>(ImmutableArray<TNode> array)
    {
        // If wrong type, just rewrite as usual
        if (this.toRemove is not TNode toRemoveOfType) return base.RewriteArray(array);

        // There is a possibility the target is in this array
        var toRemoveIndex = array.IndexOf(toRemoveOfType);

        // If the target is not in the array, just rewrite as usual
        if (toRemoveIndex == -1) return base.RewriteArray(array);

        // The target is in the array, remove the node
        return array.RemoveAt(toRemoveIndex);
    }
}
