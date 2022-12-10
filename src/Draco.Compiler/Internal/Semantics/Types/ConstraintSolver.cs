using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Types;

/// <summary>
/// Solves type-constraints.
/// </summary>
internal sealed class ConstraintSolver
{
    private enum UnificationError
    {
        TypeMismatch,
        ParameterCountMismatch,
    }

    public ImmutableArray<Diagnostic> Diagnostics => this.diagnostics.ToImmutable();

    private readonly ImmutableArray<Diagnostic>.Builder diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();

    public Type Assignable(ParseTree origin, Type to, Type from)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        this.Unify(origin, to, from);
        return to;
    }

    public Type CommonAncestor(ParseTree origin, Type to, Type from)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        this.Unify(origin, to, from);
        return to;
    }

    public Type Return(ParseTree origin, Type declared, Type returned)
    {
        // TODO: This is not the right behavior but we don't have subtyping yet
        this.Unify(origin, declared, returned);
        return declared;
    }

    public Type Same(ParseTree origin, Type t1, Type t2)
    {
        this.Unify(origin, t1, t2);
        return t1;
    }

    public Type Call(ParseTree origin, Type func, ImmutableArray<Type> args)
    {
        // TODO: This is not the right behavior but we don't have overloading yet
        var returnType = new Type.Variable(null);
        var callSite = new Type.Function(args, returnType);
        this.Unify(origin, func, callSite);
        return returnType;
    }

    private void Unify(ParseTree origin, Type left, Type right)
    {
        var result = Unify(left, right);
        if (result is not null) this.diagnostics.Add(ToDiagnostic(origin, left, right, result.Value));
    }

    private static Diagnostic ToDiagnostic(ParseTree origin, Type t1, Type t2, UnificationError error)
    {
        return Diagnostic.Create(
            template: SemanticErrors.TypeMismatch,
            location: new Location.TreeReference(origin),
            formatArgs: new[] { t1, t2 });
    }

    private static UnificationError? Unify(Type left, Type right)
    {
        static UnificationError? Ok() => null;
        static UnificationError? Error(UnificationError err) => err;

        left = UnwrapTypeVariable(left);
        right = UnwrapTypeVariable(right);

        switch (left, right)
        {
        case (Type.Variable v1, Type.Variable v2):
        {
            // Don't create a cycle
            if (!ReferenceEquals(v1, v2)) v1.Substitution = v2;
            return Ok();
        }

        // Variable substitution
        case (Type.Variable v1, _):
        {
            v1.Substitution = right;
            return Ok();
        }
        case (_, Type.Variable v2):
        {
            v2.Substitution = left;
            return Ok();
        }

        case (Type.Builtin b1, Type.Builtin b2):
        {
            if (b1.Type != b2.Type) return Error(UnificationError.TypeMismatch);
            return Ok();
        }

        case (Type.Function f1, Type.Function f2):
        {
            if (f1.Params.Length != f2.Params.Length) return Error(UnificationError.ParameterCountMismatch);
            var returnError = Unify(f1.Return, f2.Return);
            if (returnError is not null) return returnError;
            for (var i = 0; i < f1.Params.Length; ++i)
            {
                var parameterError = Unify(f1.Params[i], f2.Params[i]);
                if (parameterError is not null) return parameterError;
            }
            return Ok();
        }

        default:
        {
            return Error(UnificationError.TypeMismatch);
        }
        }
    }

    private static Type UnwrapTypeVariable(Type type) => type is Type.Variable v
        ? v.Substitution
        : type;
}
