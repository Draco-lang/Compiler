using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : TypeSymbol
{
    public override bool IsTypeVariable => true;
    public override bool IsGroundType
    {
        get
        {
            var subst = this.Substitution;
            if (subst.IsTypeVariable) return false;
            return subst.IsGroundType;
        }
    }
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

    public override string ToString()
    {
        var subst = this.Substitution;
        return subst is TypeVariable typeVar
            ? $"{StringUtils.IndexToExcelColumnName(typeVar.index)}'"
            : subst.ToString();
    }

    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();
}
