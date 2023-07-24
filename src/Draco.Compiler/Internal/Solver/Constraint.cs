using System.Collections.Generic;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Utility base-class for constraints.
/// </summary>
/// <typeparam name="TResult">The result type.</typeparam>
internal abstract class Constraint<TResult> : IConstraint<TResult>
{
    public IConstraintPromise<TResult> Promise { get; }
    IConstraintPromise IConstraint.Promise => this.Promise;
    public Diagnostic.Builder Diagnostic { get; } = new();

    protected Constraint()
    {
        this.Promise = ConstraintPromise.Create(this);
    }

    public override abstract string ToString();
}
