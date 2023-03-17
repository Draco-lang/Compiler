using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Represents a constraint that enforces two types to be the same.
/// </summary>
internal sealed class SameTypeConstraint : Constraint
{
    /// <summary>
    /// The first type that has to be the same as <see cref="Second"/>.
    /// </summary>
    public Type First { get; }

    /// <summary>
    /// The second type that has to be the same as <see cref="First"/>.
    /// </summary>
    public Type Second { get; }

    /// <summary>
    /// The promise of this constraint.
    /// </summary>
    public ConstraintPromise<Type> Promise { get; }

    public SameTypeConstraint(Type first, Type second)
    {
        this.First = first;
        this.Second = second;
        this.Promise = ConstraintPromise.FromResult(this, first);
    }
}
