using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Api.Diagnostics;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents the entire syntax tree of a Draco source file.
/// </summary>
public sealed class SyntaxTree
{
    /// <summary>
    /// The <see cref="Syntax.SourceText"/> that the tree was parsed from.
    /// </summary>
    public SourceText SourceText => throw new NotImplementedException();

    /// <summary>
    /// The root <see cref="SyntaxNode"/> of the tree.
    /// </summary>
    public SyntaxNode Root => throw new NotImplementedException();

    /// <summary>
    /// All <see cref="Diagnostic"/> messages that were produced during parsing this syntax tree.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => throw new NotImplementedException();

    /// <summary>
    /// Preorder traverses the thee with this node being the root.
    /// </summary>
    /// <returns>The enumerator that performs a preorder traversal.</returns>
    public IEnumerable<SyntaxNode> PreOrderTraverse() => this.Root.PreOrderTraverse();

    /// <summary>
    /// The internal root of the tree.
    /// </summary>
    internal Internal.Syntax.SyntaxNode GreenRoot { get; }

    private readonly ConditionalWeakTable<Internal.Syntax.SyntaxNode, Diagnostic> diagnostics;

    internal SyntaxTree(
        Internal.Syntax.SyntaxNode greenRoot,
        ConditionalWeakTable<Internal.Syntax.SyntaxNode, Diagnostic> diagnostics)
    {
        this.GreenRoot = greenRoot;
        this.diagnostics = diagnostics;
    }
}
