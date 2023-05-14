using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Solver;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base of equality comparers for symbols.
/// </summary>
internal abstract class SymbolEqualityComparer : IEqualityComparer<Symbol>, IEqualityComparer<TypeSymbol>
{
    /// <summary>
    /// A symbol equality comparer that only compares ground-types, type-variables are illegal.
    /// </summary>
    public static SymbolEqualityComparer Ground { get; } = new GroundSymbolEqualityComparer();

    /// <summary>
    /// Constructs a symbol-equality comparer that can compare types with substituted type variables.
    /// </summary>
    /// <param name="solver">The solver to use for resolving substitutions.</param>
    /// <returns>A symbol equality comparer using <paramref name="solver"/> to resolve substitutions.</returns>
    public static SymbolEqualityComparer Unwrapping(ConstraintSolver solver) =>
        new UnwrappingSymbolEqualityComparer(solver);

    public bool Equals(Symbol? x, Symbol? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;
        if (x is TypeSymbol xType && y is TypeSymbol yType) return this.Equals(xType, yType);
        return false;
    }

    public bool Equals(TypeSymbol? x, TypeSymbol? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null || y is null) return false;

        if (x is TypeVariable xTypeVar) x = this.Unwrap(xTypeVar);
        if (y is TypeVariable yTypeVar) y = this.Unwrap(yTypeVar);

        if (x.IsGenericInstance && y.IsGenericInstance)
        {
            // Generic instances might not adhere to referential equality
            // Instead we check if the generic definitions and the arguments are equal

            // TODO: Should we check for the entire context equality?
            // Could context affect the type here in any significant way, for ex. in the
            // case of nested generic types?
            // The problem with that would be that not all context variables are significant tho,
            // whe might need to end up projecting down generic args like C# does?

            if (x.GenericArguments.Length != y.GenericArguments.Length) return false;
            if (!this.Equals(x.GenericDefinition, y.GenericDefinition)) return false;
            return x.GenericArguments.SequenceEqual(y.GenericArguments, this);
        }

        return (x, y) switch
        {
            (FunctionTypeSymbol f1, FunctionTypeSymbol f2) =>
                   f1.Parameters.SequenceEqual(f2.Parameters, this)
                && this.Equals(f1.ReturnType, f2.ReturnType),
            _ => false,
        };
    }

    public int GetHashCode([DisallowNull] Symbol obj) => obj switch
    {
        TypeSymbol t => this.GetHashCode(t),
        _ => throw new ArgumentOutOfRangeException(nameof(obj)),
    };

    public int GetHashCode([DisallowNull] TypeSymbol obj)
    {
        if (obj is TypeVariable v) obj = this.Unwrap(v);

        switch (obj)
        {
        default:
            throw new ArgumentOutOfRangeException(nameof(obj));
        }
    }

    /// <summary>
    /// Unwraps the given type-variable.
    /// </summary>
    /// <param name="type">The type-variable to unwrap.</param>
    /// <returns>The substitution of <paramref name="type"/>.</returns>
    protected abstract TypeSymbol Unwrap(TypeVariable type);

    private sealed class GroundSymbolEqualityComparer : SymbolEqualityComparer
    {
        protected override TypeSymbol Unwrap(TypeVariable type) =>
            throw new InvalidOperationException("cannot compare type variables");
    }

    private sealed class UnwrappingSymbolEqualityComparer : SymbolEqualityComparer
    {
        private readonly ConstraintSolver solver;

        public UnwrappingSymbolEqualityComparer(ConstraintSolver solver)
        {
            this.solver = solver;
        }

        protected override TypeSymbol Unwrap(TypeVariable type)
        {
            var unwrappedType = this.solver.Unwrap(type);
            if (unwrappedType.IsTypeVariable) throw new InvalidOperationException("could not unwrap type variable");
            return unwrappedType;
        }
    }
}
