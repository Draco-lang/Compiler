using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.Compiler.Api.Syntax.Extensions;

/// <summary>
/// Extensions for traversing syntax nodes.
/// </summary>
public static class SyntaxNodeTraversalExtensions
{
    /// <summary>
    /// Preorder traverses the subtree.
    /// </summary>
    /// <param name="root">The root of the traversal.</param>
    /// <returns>The enumerator that performs a preorder traversal.</returns>
    public static IEnumerable<SyntaxNode> PreOrderTraverse(this SyntaxNode root)
    {
        var stk = new Stack<SyntaxNode>();
        stk.Push(root);
        while (stk.TryPop(out var node))
        {
            yield return node;
            foreach (var child in node.Children.Reverse()) stk.Push(child);
        }
    }

    /// <summary>
    /// Enumerates a subtree, yielding all descendant nodes intersecting the given index.
    /// </summary>
    /// <param name="root">The root of the subtree.</param>
    /// <param name="index">The 0-based index that has to be contained.</param>
    /// <returns>All subtree nodes containing <paramref name="index"/> in parent-child order.</returns>
    public static IEnumerable<SyntaxNode> TraverseAtIndex(this SyntaxNode root, int index)
    {
        while (true)
        {
            yield return root;
            foreach (var child in root.Children)
            {
                if (child.Span.Contains(index))
                {
                    root = child;
                    goto found;
                }
            }
            // No child found that contains position.
            break;
        found:;
        }
    }

    /// <summary>
    /// Enumerates a subtree, yielding all descendant nodes that are involved with a cursor position.
    /// This differs from <see cref="TraverseAtIndex(SyntaxNode, int)"/> in a sense, because a
    /// cursor cares about things immediately before or after it.
    /// </summary>
    /// <param name="root">The root of the subtree.</param>
    /// <param name="cursor">The position of the cursor.</param>
    /// <returns>All subtrees involved with <paramref name="cursor"/> in parent-child order.</returns>
    public static IEnumerable<SyntaxNode> TraverseAtCursorPosition(this SyntaxNode root, SyntaxPosition cursor)
    {
        while (true)
        {
            yield return root;
            foreach (var child in root.Children)
            {
                // NOTE: This allows touching at the end
                if (child.Range.Start <= cursor && cursor <= child.Range.End)
                {
                    root = child;
                    goto found;
                }
            }
            // No child found that is involved with position.
            break;
        found:;
        }
    }
}
