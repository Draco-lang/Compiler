using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : TypeSymbol, IEquatable<TypeVariable>
{
    public override bool IsTypeVariable => true;
    public override bool IsValueType => throw new NotSupportedException();
    public override bool IsError => throw new NotSupportedException();
    public override Symbol? ContainingSymbol => throw new NotSupportedException();
    public override IEnumerable<Symbol> Members => throw new NotSupportedException();
    public override string Documentation => throw new NotSupportedException();

    public override TypeSymbol Substitution => this.solver.Unwrap(this);

    private readonly ConstraintSolver solver;
    private readonly int index;

    public TypeVariable(ConstraintSolver solver, int index)
    {
        this.solver = solver;
        this.index = index;
    }

    public bool Equals(TypeVariable? other) => other is not null && this.index == other.index;
    public override int GetHashCode() => this.index.GetHashCode();
    public override bool Equals(object? obj) => this.Equals(obj as TypeVariable);

    public override string ToString() => $"{StringUtils.IndexToExcelColumnName(this.index)}'";

    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();
}
