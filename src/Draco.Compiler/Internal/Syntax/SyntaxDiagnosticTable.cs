using System;
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
    /// <summary>
    /// True, if there are any errors in the diagnostics table.
    /// </summary>
    public bool HasErrors => !this.IsDefault && this.diagnostics
        .SelectMany(diags => diags.Value)
        .Any(d => d.Info.Severity == DiagnosticSeverity.Error);

    /// <summary>
    /// True, if this table is a default, uninitialized instance.
    /// </summary>
    public bool IsDefault => this.diagnostics is null;

    private readonly ConditionalWeakTable<SyntaxNode, List<SyntaxDiagnosticInfo>> diagnostics = [];

    public SyntaxDiagnosticTable()
    {
    }

    /// <summary>
    /// Retrieves all <see cref="Diagnostic"/> messages for a given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to retrieve the <see cref="Diagnostic"/> messages for.</param>
    /// <returns>All <see cref="Diagnostic"/> messages for <paramref name="node"/>.</returns>
    public IEnumerable<Diagnostic> Get(Api.Syntax.SyntaxNode node) =>
        this.Get(node.Green).Select(diag => diag.ToDiagnostic(node));

    /// <summary>
    /// Retrieves all <see cref="SyntaxDiagnosticInfo"/> messages for a given <see cref="SyntaxNode"/>.
    /// </summary>
    /// <param name="node">The <see cref="SyntaxNode"/> to retrieve the <see cref="SyntaxDiagnosticInfo"/> messages for.</param>
    /// <returns>All <see cref="SyntaxDiagnosticInfo"/> messages for <paramref name="node"/>.</returns>
    public IReadOnlyCollection<SyntaxDiagnosticInfo> Get(SyntaxNode node) =>
        (!this.IsDefault && this.diagnostics.TryGetValue(node, out var diagnostics)) ? diagnostics : [];

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
        if (this.IsDefault) throw new InvalidOperationException("cannot add diagnostics to a default table");
        if (!this.diagnostics.TryGetValue(node, out var diagnosticList))
        {
            diagnosticList = [];
            this.diagnostics.Add(node, diagnosticList);
        }
        return diagnosticList;
    }
}
