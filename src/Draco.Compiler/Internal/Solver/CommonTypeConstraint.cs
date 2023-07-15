using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using Draco.Compiler.Internal.Binding;
using System.Collections.Immutable;

namespace Draco.Compiler.Internal.Solver;

/// <summary>
/// Constraint asserting that one type is base type of the other.
/// </summary>
internal class CommonTypeConstraint : Constraint<Unit>
{
    /// <summary>
    /// The common type of the <see cref="AlternativeTypes"/>.
    /// </summary>
    public TypeVariable CommonType { get; }

    /// <summary>
    /// The alternative types to find the <see cref="CommonType"/> of.
    /// </summary>
    public ImmutableArray<TypeSymbol> AlternativeTypes { get; }

    public CommonTypeConstraint(ConstraintSolver solver, TypeVariable commonType, ImmutableArray<TypeSymbol> alternativeTypes)
        : base(solver)
    {
        this.CommonType = commonType;
        this.AlternativeTypes = alternativeTypes;
    }

    public override string ToString() => $"CommonType({this.CommonType}, {string.Join(", ", this.AlternativeTypes)})";

    public override IEnumerable<SolveState> Solve(DiagnosticBag diagnostics)
    {
        var candidates = this.AlternativeTypes.ToList();
    while_loop:
        while (candidates.Any())
        {
            var candidate = candidates[0];
            for (int i = 0; i < this.AlternativeTypes.Length; i++)
            {
                if (!(this.Solver.Unify(candidate.Substitution, this.AlternativeTypes[i].Substitution) || IsBase(candidate.Substitution, this.AlternativeTypes[i].Substitution)))
                {
                    candidates.RemoveAt(0);
                    goto while_loop;
                }
            }
            this.Solver.Substitute(this.CommonType, candidate.Substitution);
            // Successful unification
            this.Promise.Resolve(default);
            yield return SolveState.Solved;
        }

        // Type-mismatch
        this.Diagnostic
            .WithTemplate(TypeCheckingErrors.NoCommonType)
            .WithFormatArgs(string.Join(", ", this.AlternativeTypes));
        this.Promise.Fail(default, diagnostics);
        yield return SolveState.Solved;

        bool IsBase(TypeSymbol possibleBase, TypeSymbol possibleDerived)
        {
            foreach (var baseType in possibleDerived.BaseTypes)
            {
                if (this.Solver.Unify(baseType, possibleBase)) return true;
                if (IsBase(baseType, possibleBase)) return true;
            }
            return false;
        }
    }
}
