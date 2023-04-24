using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint for the solver.
/// </summary>
internal interface IConstraint
{
    /// <summary>
    /// The solver this constraint belongs to.
    /// </summary>
    public ConstraintSolver Solver { get; }

    /// <summary>
    /// The builder for the <see cref="Api.Diagnostics.Diagnostic"/>.
    /// </summary>
    public Diagnostic.Builder Diagnostic { get; }

    /// <summary>
    /// The type-variables involved in this constraint.
    /// </summary>
    public IEnumerable<TypeVariable> TypeVariables { get; }
}
