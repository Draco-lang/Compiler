using System.Collections.Immutable;
using System.Linq;

namespace Draco.Compiler.Internal.Syntax.Rewriting;

/// <summary>
/// A syntax rewriter that inserts a node before another node.
/// </summary>
internal sealed class InsertRewriter : SyntaxRewriter
{
    /// <summary>
    /// Inserts a node before another node.
    /// </summary>
    /// <param name="root">The root node to rewrite.</param>
    /// <param name="toInsert">The node to insert.</param>
    /// <param name="target">The node to insert before.</param>
    /// <returns>The rewritten root node.</returns>
    public static SyntaxNode InsertBefore(SyntaxNode root, SyntaxNode toInsert, SyntaxNode target) =>
        root.Accept(new InsertRewriter(toInsert, target, before: true));

    /// <summary>
    /// Inserts a node after another node.
    /// </summary>
    /// <param name="root">The root node to rewrite.</param>
    /// <param name="toInsert">The node to insert.</param>
    /// <param name="target">The node to insert after.</param>
    /// <returns>The rewritten root node.</returns>
    public static SyntaxNode InsertAfter(SyntaxNode root, SyntaxNode toInsert, SyntaxNode target) =>
        root.Accept(new InsertRewriter(toInsert, target, before: false));

    private readonly SyntaxNode toInsert;
    private readonly SyntaxNode target;
    private readonly bool before;

    private InsertRewriter(SyntaxNode toInsert, SyntaxNode target, bool before)
    {
        this.toInsert = toInsert;
        this.target = target;
        this.before = before;
    }

    protected override ImmutableArray<TNode>? RewriteArray<TNode>(ImmutableArray<TNode> array)
    {
        // If wrong type, just rewrite as usual
        if (this.target is not TNode targetOfType) return base.RewriteArray(array);

        // There is a possibility the target is in this array
        var targetIndex = array.IndexOf(targetOfType);

        // If the target is not in the array, just rewrite as usual
        if (targetIndex == -1) return base.RewriteArray(array);

        // The target is in the array, insert the node
        return array.Insert(targetIndex + (this.before ? 0 : 1), (TNode)(SyntaxNode)this.toInsert);
    }
}
