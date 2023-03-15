using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
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
    internal virtual Symbol BindLabel(LabelSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
    {
        NameLabelSyntax name => this.BindNameLabel(name, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameLabel(NameLabelSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // TODO: Value? We could explicitly look for labels...
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return symbol switch
        {
            LabelSymbol l => l,
            _ => throw new InvalidOperationException(),
        };
    }
}
