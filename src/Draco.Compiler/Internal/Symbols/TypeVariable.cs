using System;
using System.Collections.Generic;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Solver.Tasks;
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
    public override IEnumerable<Symbol> DefinedMembers => throw new NotSupportedException();

    public override TypeSymbol Substitution
    {
        get
        {
            if (this.substitution is null) return this;
            // Pruning
            if (this.substitution is TypeVariable tv) this.substitution = tv.Substitution;
            return this.substitution;
        }
    }

    /// <summary>
    /// A task that completes when this variable is substituted.
    /// </summary>
    public SolverTask<TypeSymbol> Substituted => this.substitutedCompletionSource.Task;

    private readonly SolverTaskCompletionSource<TypeSymbol> substitutedCompletionSource = new();
    private TypeSymbol? substitution;
    private readonly int index;

    public TypeVariable(int index)
    {
        this.index = index;
    }

    public override string ToString() => this.Substitution switch
    {
        TypeVariable typeVar => $"{StringUtils.IndexToExcelColumnName(typeVar.index)}'",
        var t => t.ToString(),
    };

    public override void Accept(SymbolVisitor visitor) => throw new NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new NotSupportedException();

    public void Substitute(TypeSymbol other)
    {
        if (this.substitution is not null) throw new InvalidOperationException("type variable already substituted");
        this.substitution = other;
        this.substitutedCompletionSource.SetResult(other);
    }
}
