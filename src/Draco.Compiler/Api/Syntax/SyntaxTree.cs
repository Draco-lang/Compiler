using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Syntax;
using Draco.Compiler.Internal.Syntax.Formatting;
using Draco.Compiler.Internal.Syntax.Rewriting;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents the entire syntax tree of a Draco source file.
/// </summary>
public sealed class SyntaxTree
{
    /// <summary>
    /// Constructs a new <see cref="SyntaxTree"/> with given <paramref name="path"/> from the given <paramref name="root"/>, if <paramref name="path"/> is null, there will be no source text set.
    /// </summary>
    /// <param name="root">The root of the tree.</param>
    /// <returns>A new <see cref="SyntaxTree"/> with <see cref="Root"/> <paramref name="root"/> and <see cref="SourceText.Path"/> <paramref name="path"/>.</returns>
    public static SyntaxTree Create(SyntaxNode root, string? path = null) =>
        new(sourceText: path is null ? SourceText.None : SourceText.FromText(new Uri(path), ReadOnlyMemory<char>.Empty), greenRoot: root.Green, syntaxDiagnostics: new());

    /// <summary>
    /// Parses the given text into a <see cref="SyntaxTree"/> with <see cref="SourceText.Path"/> <paramref name="path"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <param name="path">The path this tree comes from.</param>
    /// <returns>The parsed tree.</returns>
    public static SyntaxTree Parse(string source, string? path = null) =>
        Parse(path is null ? SourceText.FromText(source.AsMemory()) : SourceText.FromText(new Uri(path), source.AsMemory()));

    /// <summary>
    /// Parses the given <see cref="Syntax.SourceText"/> into a <see cref="SyntaxTree"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <returns>The parsed tree.</returns>
    public static SyntaxTree Parse(SourceText source)
    {
        var diags = new SyntaxDiagnosticTable();
        var srcReader = source.SourceReader;
        var lexer = new Lexer(srcReader, diags);
        var tokenSource = TokenSource.From(lexer);
        var parser = new Parser(tokenSource, diags);
        var cu = parser.ParseCompilationUnit();
        return new(source, cu, diags);
    }

    /// <summary>
    /// The <see cref="Syntax.SourceText"/> that the tree was parsed from.
    /// </summary>
    public SourceText SourceText { get; }

    /// <summary>
    /// The root <see cref="SyntaxNode"/> of the tree.
    /// </summary>
    public SyntaxNode Root =>
        LazyInitializer.EnsureInitialized(ref this.root, () => this.GreenRoot.ToRedNode(this, null, 0));
    private SyntaxNode? root;

    /// <summary>
    /// All <see cref="Diagnostic"/> messages that were produced during parsing this syntax tree.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this
        .PreOrderTraverse()
        .SelectMany(n => n.Diagnostics);

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
    /// Enumerates this tree, yielding all descendant nodes containing the given index.
    /// </summary>
    /// <param name="index">The 0-based index that has to be contained.</param>
    /// <returns>All subtree nodes containing <paramref name="index"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesAtIndex(int index) => this.Root.TraverseSubtreesAtIndex(index);

    /// <summary>
    /// Enumerates this subtree, yielding all descendant nodes intersecting the given span.
    /// </summary>
    /// <param name="span">The span to check for intersection with the nodes.</param>
    /// <returns>All subtrees in intersecting <paramref name="span"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesIntersectingSpan(SourceSpan span) => this.Root.TraverseSubtreesIntersectingSpan(span);

    /// <summary>
    /// Enumerates this tree, yielding all descendant nodes containing the given position.
    /// </summary>
    /// <param name="position">The position that has to be contained.</param>
    /// <returns>All subtree nodes containing <paramref name="position"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(SyntaxPosition position) => this.Root.TraverseSubtreesAtPosition(position);

    /// <summary>
    /// Enumerates this subtree, yielding all descendant nodes intersecting the given range.
    /// </summary>
    /// <param name="range">The range to check for intersection with the nodes.</param>
    /// <returns>All subtrees in intersecting <paramref name="range"/> in parent-child order.</returns>
    public IEnumerable<SyntaxNode> TraverseSubtreesIntersectingRange(SyntaxRange range) => this.Root.TraverseSubtreesIntersectingRange(range);

    /// <summary>
    /// Reorders the <see cref="SyntaxTree"/> that contains <paramref name="toReorder"/> node and puts <paramref name="toReorder"/> to specified <paramref name="position"/> in the original <see cref="SyntaxList"/>.
    /// </summary>
    /// <param name="toReorder">The <see cref="SyntaxNode"/> that will be reordered.</param>
    /// <param name="position">The position in the original <see cref="SyntaxList"/> where <paramref name="toReorder"/> node will be put.</param>
    /// <returns>New constructed <see cref="SyntaxTree"/> with <paramref name="toReorder"/> at new <paramref name="position"/>.</returns>
    public SyntaxTree Reorder(SyntaxNode toReorder, int position) => new(
        this.SourceText,
        this.GreenRoot.Accept(new ReorderRewriter(toReorder.Green, position)),
        new());

    /// <summary>
    /// Removes the <paramref name="toRemove"/> node from the <see cref="SyntaxList"/> <paramref name="toRemove"/> is contained in.
    /// </summary>
    /// <param name="toRemove">The <see cref="SyntaxNode"/> that will be removed.</param>
    /// <returns>New constructed <see cref="SyntaxTree"/> with <paramref name="toRemove"/> remove from the <see cref="SyntaxTree"/>.</returns>
    public SyntaxTree Remove(SyntaxNode toRemove) => new(
        this.SourceText,
        this.GreenRoot.Accept(new RemoveRewriter(toRemove.Green)),
        new());

    /// <summary>
    /// Inserts the <paramref name="toInsert"/> node to <paramref name="insertInto"/> at specified <paramref name="position"/> if <paramref name="insertInto"/> is a <see cref="SyntaxList"/>.
    /// </summary>
    /// <param name="toInsert">The <see cref="SyntaxNode"/> that will be inserted.</param>
    /// <param name="insertInto">The <see cref="SyntaxNode"/> <paramref name="toInsert"/> will be inserted to.</param>
    /// <param name="position">The position <paramref name="toInsert"/> node will be put.</param>
    /// <returns>New constructed <see cref="SyntaxTree"/> with <paramref name="toInsert"/> inserted into <paramref name="insertInto"/>.</returns>
    public SyntaxTree Insert(SyntaxNode toInsert, SyntaxNode insertInto, int position) => new(
        this.SourceText,
        this.GreenRoot.Accept(new InsertRewriter(toInsert.Green, insertInto.Green, position)),
        new());

    /// <summary>
    /// Returns the difference between this <see cref="SyntaxTree"/> and <paramref name="other"/>.
    /// </summary>
    /// <param name="other">The other <see cref="SyntaxTree"/> to find differences with this tree.</param>
    /// <returns>Array of <see cref="TextEdit"/>s.</returns>
    public ImmutableArray<TextEdit> SyntaxTreeDiff(SyntaxTree other) =>
        // TODO: We can use a better diff algo
        ImmutableArray.Create(new TextEdit(this.Root.Range, other.ToString()));

    /// <summary>
    /// Syntactically formats this <see cref="SyntaxTree"/>.
    /// </summary>
    /// <returns>The formatted tree.</returns>
    public string Format(FormatterSettings? settings = null) => DracoFormatter.Format(this, settings);

    /// <summary>
    /// The internal root of the tree.
    /// </summary>
    internal Internal.Syntax.SyntaxNode GreenRoot { get; }

    /// <summary>
    /// The table where internal diagnostics are written to.
    /// </summary>
    internal SyntaxDiagnosticTable SyntaxDiagnosticTable { get; }

    internal SyntaxTree(
        SourceText sourceText,
        Internal.Syntax.SyntaxNode greenRoot,
        SyntaxDiagnosticTable syntaxDiagnostics)
    {
        this.SourceText = sourceText;
        this.GreenRoot = greenRoot;
        this.SyntaxDiagnosticTable = syntaxDiagnostics;
    }

    /// <summary>
    /// Constructs a DOT representation of this syntax tree.
    /// </summary>
    /// <returns>The DOT code of this syntax tree.</returns>
    public string ToDot() => this.GreenRoot.ToDot();

    public override string ToString() => this.Root.ToString();
}
