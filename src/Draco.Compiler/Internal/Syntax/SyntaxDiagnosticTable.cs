using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A container for syntax diagnostic messages.
/// </summary>
internal readonly struct SyntaxDiagnosticTable
{
    /// <summary>
    /// All <see cref="Diagnostic"/> messages attached to some <see cref="SyntaxNode"/>.
    /// </summary>
    public IEnumerable<Diagnostic> Diagnostics => this.diagnostics.SelectMany(kv => kv.Value);

    private readonly ConditionalWeakTable<SyntaxNode, List<Diagnostic>> diagnostics = new();

    public SyntaxDiagnosticTable()
    {
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/> messages for a given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to retrieve the <see cref="Diagnostic"/> messages for.</param>
    /// <returns>All <see cref="Diagnostic"/> messages for <paramref name="node"/>.</returns>
    public IReadOnlyCollection<Diagnostic> Get(SyntaxNode node) => this.diagnostics.TryGetValue(node, out var diagnostics)
        ? diagnostics
        : ImmutableArray<Diagnostic>.Empty;

    /// <summary>
    /// Adds a <see cref="Diagnostic"/> to the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to attach <paramref name="diagnostic"/> to.</param>
    /// <param name="diagnostic">The <see cref="Diagnostic"/> to attach to <paramref name="node"/>.</param>
    public void Add(SyntaxNode node, Diagnostic diagnostic)
    {
        if (!this.diagnostics.TryGetValue(node, out var diagnosticList))
        {
            diagnosticList = new();
            this.diagnostics.Add(node, diagnosticList);
        }
        diagnosticList.Add(diagnostic);
    }

    /// <summary>
    /// Adds a list <see cref="Diagnostic"/>s to the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to attach the <paramref name="diagnostics"/> to.</param>
    /// <param name="diagnostics">The <see cref="Diagnostic"/>s to attach to <paramref name="node"/>.</param>
    public void Add(SyntaxNode node, IEnumerable<Diagnostic> diagnostics)
    {
        if (!this.diagnostics.TryGetValue(node, out var diagnosticList))
        {
            diagnosticList = new();
            this.diagnostics.Add(node, diagnosticList);
        }
        diagnosticList.AddRange(diagnostics);
    }
}
