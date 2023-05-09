using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

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

    /// <summary>
    /// The type of the member.
    /// </summary>
    public TypeSymbol MemberType { get; }

    public MemberConstraint(ConstraintSolver solver, TypeSymbol accessed, string memberName, TypeSymbol memberType)
        : base(solver)
    {
        this.Accessed = accessed;
        this.MemberName = memberName;
        this.MemberType = memberType;
    }

    public override string ToString() => $"Member({this.Accessed}, {this.MemberName})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
    start:
        var accessed = this.Unwrap(this.Accessed);
        // We can't advance on type variables
        if (accessed.IsTypeVariable)
        {
            yield return SolveState.Stale;
            goto start;
        }

        // Not a type variable, we can look into members
        var membersWithName = accessed.Members
            .Where(m => m.Name == this.MemberName)
            .Where(m => m is ITypedSymbol s && !s.IsStatic)
            .ToImmutableArray();

        if (membersWithName.Length == 0)
        {
            // No such member, error
            this.Diagnostic
                .WithTemplate(SymbolResolutionErrors.MemberNotFound)
                .WithFormatArgs(this.MemberName, this.Unwrap(this.Accessed));
            // We still provide a single error symbol
            var errorSymbol = new UndefinedMemberSymbol();
            this.Promise.Fail(ImmutableArray.Create<Symbol>(errorSymbol), diagnostics);
            yield return SolveState.Solved;
        }
        else if (membersWithName.Length == 1)
        {
            // One member, we know what type the member type is
            this.Unify(((ITypedSymbol)membersWithName[0]).Type, this.MemberType);
            this.Promise.Resolve(membersWithName);
            yield return SolveState.Solved;
        }
        else
        {
            // More than one, the member constraint is fine with multiple members but we don't know the member type
            this.Unify(ErrorTypeSymbol.Instance, this.MemberType);
            this.Promise.Resolve(membersWithName);
            yield return SolveState.Solved;
        }
    }
}
