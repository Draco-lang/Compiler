using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// A container for syntax diagnostic messages.
/// </summary>
internal readonly struct SyntaxDiagnosticTable
{
    private readonly ConditionalWeakTable<SyntaxNode, List<SyntaxDiagnosticInfo>> diagnostics = new();

    public SyntaxDiagnosticTable()
    {
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/> messages for a given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to retrieve the <see cref="Diagnostic"/> messages for.</param>
    /// <returns>All <see cref="Diagnostic"/> messages for <paramref name="node"/>.</returns>
    public IEnumerable<Diagnostic> Get(Api.Syntax.SyntaxNode node) => this.diagnostics.TryGetValue(node.Green, out var diagnostics)
        ? diagnostics.Select(diag => diag.ToDiagnostic(node))
        : Enumerable.Empty<Diagnostic>();

    /// <summary>
    /// Adds a <see cref="SyntaxDiagnosticInfo"/> to the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to attach <paramref name="diagnostic"/> to.</param>
    /// <param name="diagnostic">The <see cref="SyntaxDiagnosticInfo"/> to attach to <paramref name="node"/>.</param>
    public void Add(SyntaxNode node, SyntaxDiagnosticInfo diagnostic) =>
        this.GetDiagnosticList(node).Add(diagnostic);

    /// <summary>
    /// Adds a range of <see cref="SyntaxDiagnosticInfo"/>s to the given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to attach the <paramref name="diagnostics"/> to.</param>
    /// <param name="diagnostics">The <see cref="SyntaxDiagnosticInfo"/>s to attach to <paramref name="node"/>.</param>
    public void AddRange(SyntaxNode node, IEnumerable<SyntaxDiagnosticInfo> diagnostics) =>
        this.GetDiagnosticList(node).AddRange(diagnostics);

    private List<SyntaxDiagnosticInfo> GetDiagnosticList(SyntaxNode node)
    {
        if (!this.diagnostics.TryGetValue(node, out var diagnosticList))
        {
            diagnosticList = new();
            this.diagnostics.Add(node, diagnosticList);
        }
        return diagnosticList;
    }
}
