using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constraint asserting that two types have to be exactly the same.
/// </summary>
internal sealed class SameTypeConstraint : IConstraint
{
    public ConstraintSolver Solver { get; }
    public IConstraintPromise Promise { get; }
    public Diagnostic.Builder Diagnostic { get; } = new();

    /// <summary>
    /// The type that has to be the same as <see cref="Second"/>.
    /// </summary>
    public TypeSymbol First { get; }

    /// <summary>
    /// The type that has to be the same as <see cref="First"/>.
    /// </summary>
    public TypeSymbol Second { get; }

    public IEnumerable<TypeVariable> TypeVariables
    {
        get
        {
            if (this.First is TypeVariable tv1) yield return tv1;
            if (this.Second is TypeVariable tv2) yield return tv2;
        }
    }

    public SameTypeConstraint(ConstraintSolver solver, TypeSymbol first, TypeSymbol second)
    {
        this.Solver = solver;
        this.First = first;
        this.Second = second;
        this.Promise = ConstraintPromise.Create<Unit>(this);
    }

    public override string ToString() => $"SameType({this.First}, {this.Second})";

    public SolveState Solve() => throw new NotImplementedException();
}
