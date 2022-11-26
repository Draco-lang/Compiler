using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// Solves type-constraints.
/// </summary>
internal sealed class ConstraintSolver
{
    public void Assignable(Type to, Type from)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        Unify(to, from);
    }

    private static Type Unify(Type left, Type right)
    {
        left = UnwrapTypeVariable(left);
        right = UnwrapTypeVariable(right);

        throw new NotImplementedException();
    }

    private static Type UnwrapTypeVariable(Type type) => type is Type.Variable v
        ? v.Substitution
        : type;
}
