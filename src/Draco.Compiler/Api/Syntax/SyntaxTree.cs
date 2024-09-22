using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax.Extensions;
using Draco.Compiler.Internal.Syntax;

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
    public static SyntaxTree Create(SyntaxNode root, string? path = null) => Create(root.Green, path);

    internal static SyntaxTree Create(
        Internal.Syntax.SyntaxNode root,
        SyntaxDiagnosticTable syntaxDiagnostics) => Create(root: root, path: null as Uri, syntaxDiagnostics: syntaxDiagnostics);

    internal static SyntaxTree Create(
        Internal.Syntax.SyntaxNode root,
        string? path = null,
        SyntaxDiagnosticTable syntaxDiagnostics = default) => Create(
            root: root,
            path: path is null ? null : new Uri(path),
            syntaxDiagnostics: syntaxDiagnostics);

    internal static SyntaxTree Create(
        Internal.Syntax.SyntaxNode root,
        Uri? path = null,
        SyntaxDiagnosticTable syntaxDiagnostics = default) => new(
        sourceText: SourceText.FromText(
            path: path,
            text: root.ToCode().AsMemory()),
        greenRoot: root,
        syntaxDiagnostics: syntaxDiagnostics);

    /// <summary>
    /// Parses the given text into a <see cref="SyntaxTree"/> with <see cref="SourceText.Path"/> <paramref name="path"/>.
    /// </summary>
    /// <param name="source">The source to parse.</param>
    /// <param name="path">The path this tree comes from.</param>
    /// <returns>The parsed tree.</returns>
    public static SyntaxTree Parse(string source, string? path = null) =>
        Parse(SourceText.FromText(path is null ? null : new Uri(path), source.AsMemory()));

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
    /// Parses a script from the given <see cref="ISourceReader"/>.
    /// </summary>
    /// <param name="sourceReader">The source reader to parse from.</param>
    /// <returns>The parsed tree.</returns>
    internal static SyntaxTree ParseScript(ISourceReader sourceReader)
    {
        var syntaxDiagnostics = new SyntaxDiagnosticTable();

        // Construct a lexer
        var lexer = new Lexer(sourceReader, syntaxDiagnostics);
        // Construct a token source
        var tokenSource = TokenSource.From(lexer);
        // Construct a parser
        var parser = new Parser(tokenSource, syntaxDiagnostics, parserMode: ParserMode.Repl);
        // Parse a repl entry
        var node = parser.ParseScriptEntry();
        // Make it into a tree
        return Create(node, syntaxDiagnostics: syntaxDiagnostics);
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
    /// True, if the tree has any errors.
    /// </summary>
    public bool HasErrors => this.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// All <see cref="Diagnostic"/> messages that were produced during parsing this syntax tree.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this.Root
        .PreOrderTraverse()
        .SelectMany(n => n.Diagnostics);

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
