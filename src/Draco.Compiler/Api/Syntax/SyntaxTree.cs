using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Constructs a new <see cref="SyntaxTree"/> from the given <paramref name="root"/>.
    /// </summary>
    /// <param name="root">The root of the tree.</param>
    /// <returns>A new <see cref="SyntaxTree"/> with <see cref="Root"/> <paramref name="root"/>.</returns>
    public static SyntaxTree Create(SyntaxNode root) =>
        new(sourceText: SourceText.None, greenRoot: root.Green, syntaxDiagnostics: new());

    /// <summary>
    /// Parses the given text into a <see cref="SyntaxTree"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <returns>The parsed tree.</returns>
    public static SyntaxTree Parse(string source) => Parse(SourceText.FromText(source));

    /// <summary>
    /// Parses the given <see cref="Syntax.SourceText"/> into a <see cref="SyntaxTree"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <returns>The parsed tree.</returns>
    public static SyntaxTree Parse(SourceText source)
    {
        var srcReader = source.SourceReader;
        var lexer = new Internal.Syntax.Lexer(srcReader);
        var tokenSource = Internal.Syntax.TokenSource.From(lexer);
        var parser = new Internal.Syntax.Parser(tokenSource);
        var cu = parser.ParseCompilationUnit();
        // TODO: Pass in diags
        return new(source, cu, new());
    }

    /// <summary>
    /// The <see cref="Syntax.SourceText"/> that the tree was parsed from.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    /// The root <see cref="SyntaxNode"/> of the tree.
    /// </summary>
    public SyntaxNode Root => this.GreenRoot.ToRedNode(this, null);

    /// <summary>
    /// All <see cref="Diagnostic"/> messages that were produced during parsing this syntax tree.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this.syntaxDiagnostics.Select(kv => kv.Value);

    /// <summary>
    /// Preorder traverses the thee with this node being the root.
    /// </summary>
    /// <returns>The enumerator that performs a preorder traversal.</returns>
    public IEnumerable<SyntaxNode> PreOrderTraverse() => this.Root.PreOrderTraverse();

    /// <summary>
    /// Searches for a child node of type <typeparamref name="TNode"/>.
    /// </summary>
    /// <typeparam name="TNode">The type of child to search for.</typeparam>
    /// <param name="index">The index of the child to search for.</param>
    /// <returns>The <paramref name="index"/>th child of type <typeparamref name="TNode"/>.</returns>
    public TNode FindInChildren<TNode>(int index = 0)
        where TNode : SyntaxNode => this.Root.FindInChildren<TNode>(index);

    /// <summary>
    /// Enumerates this tree, yielding all descendant nodes containing the given position.
    /// </summary>
    /// <param name="position">The position that has to be contained.</param>
    /// <returns>All subtree nodes containing <paramref name="position"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(Position position) => this.Root.TraverseSubtreesAtPosition(position);

    /// <summary>
    /// The internal root of the tree.
    /// </summary>
    internal Internal.Syntax.SyntaxNode GreenRoot { get; }

    private readonly ConditionalWeakTable<Internal.Syntax.SyntaxNode, Diagnostic> syntaxDiagnostics;

    internal SyntaxTree(
        SourceText sourceText,
        Internal.Syntax.SyntaxNode greenRoot,
        ConditionalWeakTable<Internal.Syntax.SyntaxNode, Diagnostic> syntaxDiagnostics)
    {
        this.SourceText = sourceText;
        this.GreenRoot = greenRoot;
        this.syntaxDiagnostics = syntaxDiagnostics;
    }
}
