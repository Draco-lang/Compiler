using System;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to a label symbol.
    /// </summary>
    /// <param name="syntax">The type to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The looked up label symbol for <paramref name="syntax"/>.</returns>
    internal virtual Symbol BindLabel(LabelSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        NameLabelSyntax name => this.BindNameLabel(name, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameLabel(NameLabelSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        this.LookupLabelSymbol(syntax.Name.Text, syntax, diagnostics);
}
