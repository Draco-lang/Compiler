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
    /// Binds the given syntax node to a type symbol.
    /// </summary>
    /// <param name="syntax">The type to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The looked up type symbol for <paramref name="syntax"/>.</returns>
    protected Symbol BindType(TypeSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
    {
        NameTypeSyntax name => this.BindNameType(name, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private Symbol BindNameType(NameTypeSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupTypeSymbol(syntax.Name.Text, syntax, diagnostics);
        return symbol switch
        {
            _ => throw new InvalidOperationException(),
        };
    }
}
