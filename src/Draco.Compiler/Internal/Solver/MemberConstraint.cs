using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// A constaint representing that a type needs to have a given member.
/// </summary>
internal sealed class MemberConstraint : Constraint<ImmutableArray<Symbol>>
{
    /// <summary>
    /// The accessed symbol type.
    /// </summary>
    public TypeSymbol Accessed { get; }

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; }

    public override IEnumerable<TypeVariable> TypeVariables
    {
        get
        {
            if (this.Accessed is TypeVariable tv) yield return tv;
        }
    }

    public MemberConstraint(ConstraintSolver solver, TypeSymbol accessed, string memberName)
        : base(solver)
    {
        this.Accessed = accessed;
        this.MemberName = memberName;
    }

    public override string ToString() => $"Member({this.Accessed}, {this.MemberName})";

    public override SolveState Solve(DiagnosticBag diagnostics)
    {
        var accessed = this.Unwrap(this.Accessed);
        // We can't advance on type variables
        if (accessed.IsTypeVariable) return SolveState.Stale;

        // Not a type variable, we can look into members
        var membersWithName = accessed.Members
            .Where(m => m.Name == this.MemberName)
            .ToImmutableArray();

        if (membersWithName.Length == 0)
        {
            // No such member, error
            this.Diagnostic
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(this.MemberName, this.Unwrap(this.Accessed));
            this.Promise.Fail(membersWithName, diagnostics);
            return SolveState.Solved;
        }
        else
        {
            // One or more, the member constraint is fine with multiple members
            this.Promise.Resolve(membersWithName);
            return SolveState.Solved;
        }
    }
}
