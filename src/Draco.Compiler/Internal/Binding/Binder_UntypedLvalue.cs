using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped lvalue.
    /// </summary>
    /// <param name="syntax">The lvalue to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped lvalue for <paramref name="syntax"/>.</returns>
    protected UntypedLvalue BindLvalue(SyntaxNode syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
    {
        NameExpressionSyntax name => this.BindNameLvalue(name, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedLvalue BindNameLvalue(NameExpressionSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var lookup = this.LookupValueSymbol(syntax.Name.Text, syntax);
        if (!lookup.FoundAny)
        {
            // TODO
            throw new NotImplementedException();
        }
        if (lookup.Symbols.Count > 1)
        {
            // TODO: Multiple symbols, potental overloading
            throw new NotImplementedException();
        }
        return lookup.Symbols[0] switch
        {
            LocalSymbol local => new UntypedLocalLvalue(syntax, local),
            _ => throw new InvalidOperationException(),
        };
    }
}
