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
    public Type Assignable(Type to, Type from)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        Unify(to, from);
        return to;
    }

    public Type CommonAncestor(Type to, Type from)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        Unify(to, from);
        return to;
    }

    public Type Same(Type t1, Type t2)
    {
        Unify(t1, t2);
        return t1;
    }

    private static void Unify(Type left, Type right)
    {
        left = UnwrapTypeVariable(left);
        right = UnwrapTypeVariable(right);

        switch (left, right)
        {
        case (Type.Variable v1, Type.Variable v2):
            // Don't create a cycle
            if (ReferenceEquals(v1, v2)) break;
            v1.Substitution = v2;
            break;

        // Variable substitution
        case (Type.Variable v1, _):
            v1.Substitution = right;
            break;
        case (_, Type.Variable v2):
            v2.Substitution = left;
            break;

        case (Type.Builtin b1, Type.Builtin b2):
            // TODO: Type error
            if (b1.Type != b2.Type) throw new NotImplementedException();
            break;

        default:
            // TODO
            throw new NotImplementedException();
        }
    }

    private static Type UnwrapTypeVariable(Type type) => type is Type.Variable v
        ? v.Substitution
        : type;
}
