using System;
using System.Collections.Generic;
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
    private readonly ConditionalWeakTable<SyntaxNode, List<Diagnostic>> diagnostics = new();

    public SyntaxDiagnosticTable()
    {
    }

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
}
