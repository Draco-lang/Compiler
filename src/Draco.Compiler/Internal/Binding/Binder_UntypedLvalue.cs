using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
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
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return symbol switch
        {
            UntypedLocalSymbol local => new UntypedLocalLvalue(syntax, local, constraints.LocalReference(local, syntax)),
            _ => throw new InvalidOperationException(),
        };
    }
}
