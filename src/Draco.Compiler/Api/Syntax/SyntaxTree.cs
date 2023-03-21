using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Syntax;

namespace Draco.Compiler.Api.Syntax;

/// <summary>
/// Represents the entire syntax tree of a Draco source file.
/// </summary>
public sealed class SyntaxTree
{
    private static SyntaxTreeFormatterSettings FormatterSettings { get; } = new(
        Indentation: "    ");

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
    public SyntaxNode Root => this.root ??= this.GreenRoot.ToRedNode(this, null);
    private SyntaxNode? root;

    /// <summary>
    /// All <see cref="Diagnostic"/> messages that were produced during parsing this syntax tree.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this
        .PreOrderTraverse()
        .SelectMany(this.syntaxDiagnostics.Get);

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
    public IEnumerable<SyntaxNode> TraverseSubtreesAtPosition(SyntaxPosition position) => this.Root.TraverseSubtreesAtPosition(position);

    /// <summary>
    /// Syntactically formats this <see cref="SyntaxTree"/>.
    /// </summary>
    /// <returns>The formatted tree.</returns>
    public SyntaxTree Format() => new SyntaxTreeFormatter(FormatterSettings).Format(this);

    /// <summary>
    /// The internal root of the tree.
    /// </summary>
    internal Internal.Syntax.SyntaxNode GreenRoot { get; }

    private readonly SyntaxDiagnosticTable syntaxDiagnostics;

    internal SyntaxTree(
        SourceText sourceText,
        Internal.Syntax.SyntaxNode greenRoot,
        SyntaxDiagnosticTable syntaxDiagnostics)
    {
        this.SourceText = sourceText;
        this.GreenRoot = greenRoot;
        this.syntaxDiagnostics = syntaxDiagnostics;
    }

    public override string ToString() => this.Root.ToString();

    internal void ComputeFullPositions()
    {
        var position = 0;
        foreach (var node in this.Root.PreOrderTraverse())
        {
            node.SetFullPosition(position);
            if (node is SyntaxToken token) position += token.Green.FullWidth;
        }
    }
}
