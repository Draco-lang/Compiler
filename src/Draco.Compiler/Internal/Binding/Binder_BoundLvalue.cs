using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given untyped lvalue to a bound lvalue.
    /// </summary>
    /// <param name="lvalue">The untyped lvalue to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The bound lvalue for <paramref name="lvalue"/>.</returns>
    internal virtual BoundLvalue TypeLvalue(UntypedLvalue lvalue, ConstraintBag constraints, DiagnosticBag diagnostics) => lvalue switch
    {
        UntypedLocalLvalue local => this.TypeLocalLvalue(local, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(lvalue)),
    };

    private BoundLvalue TypeLocalLvalue(UntypedLocalLvalue local, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new BoundLocalLvalue(local.Syntax, constraints.GetTypedLocal(local.Local));
}
