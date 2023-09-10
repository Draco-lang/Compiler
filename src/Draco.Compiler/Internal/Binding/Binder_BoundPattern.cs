using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given untyped pattern to a bound pattern.
    /// </summary>
    /// <param name="pattern">The untyped pattern to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The bound expression for <paramref name="pattern"/>.</returns>
    internal virtual BoundPattern TypePattern(UntypedPattern pattern, ConstraintSolver constraints, DiagnosticBag diagnostics) => pattern switch
    {
        UntypedDiscardPattern discard => this.TypeDiscardPattern(discard, constraints, diagnostics),
        UntypedLiteralPattern literal => this.TypeLiteralPattern(literal, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(pattern)),
    };

    private BoundPattern TypeDiscardPattern(UntypedDiscardPattern discard, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundDiscardPattern(discard.Syntax);

    private BoundPattern TypeLiteralPattern(UntypedLiteralPattern literal, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundLiteralPattern(literal.Syntax, literal.Value, literal.Type);
}
