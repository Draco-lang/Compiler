using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// The possible states of a variable in terms of definite assignment.
/// </summary>
internal enum AssignementState
{
    /// <summary>
    /// The variable is definitely assigned.
    /// </summary>
    DefinitelyAssigned,

    /// <summary>
    /// The variable is definitely unassigned.
    /// </summary>
    DefinitelyUnassigned,

    /// <summary>
    /// The variable might have been assigned.
    /// </summary>
    MaybeAssigned,
}

/// <summary>
/// A domain for performing definite assignment analysis.
/// This analysis is a little more elaborate than in CS courses, where it's generally presented as a gen-kill
/// problem and is implemented with bit-vectors. In our case, we don't just want to know if a variable might
/// not have been initialized, but also if it MUST have been initialized for immutable variables.
/// This essentially shatters the state space into 3 states: definitely assigned, definitely unassigned, and
/// maybe assigned. For this reason, we can't use a bit for each variable state.
/// </summary>
internal sealed class DefiniteAssignmentDomain(ImmutableArray<LocalSymbol> locals)
    : FlowDomain<Dictionary<LocalSymbol, AssignementState>>
{
    /// <summary>
    /// The locals being tracked.
    /// </summary>
    public ImmutableArray<LocalSymbol> Locals { get; } = locals;

    public override FlowDirection Direction => FlowDirection.Forward;
    public override Dictionary<LocalSymbol, AssignementState> Top =>
        this.Locals.ToDictionary(l => l, _ => AssignementState.DefinitelyUnassigned);

    public override bool Equals(Dictionary<LocalSymbol, AssignementState> state1, Dictionary<LocalSymbol, AssignementState> state2)
    {
        if (state1.Count != state2.Count) return false;
        foreach (var (key, value) in state1)
        {
            if (!state2.TryGetValue(key, out var otherValue)) return false;
            if (value != otherValue) return false;
        }
        return true;
    }

    public override Dictionary<LocalSymbol, AssignementState> Clone(in Dictionary<LocalSymbol, AssignementState> state) =>
        state.ToDictionary(kv => kv.Key, kv => kv.Value);

    public override string ToString(Dictionary<LocalSymbol, AssignementState> state) =>
        $"[{string.Join(", ", state.OrderBy(kv => kv.Key.Name).Select(kv => $"{kv.Key.Name}: {kv.Value}"))}]";

    public override void Join(
        ref Dictionary<LocalSymbol, AssignementState> target,
        IEnumerable<Dictionary<LocalSymbol, AssignementState>> sources)
    {
        target.Clear();
        foreach (var local in this.Locals)
        {
            var localState = null as AssignementState?;
            var localStates = sources.Select(d => d.TryGetValue(local, out var s) ? s : AssignementState.DefinitelyUnassigned);
            foreach (var state in localStates)
            {
                localState = localState is null ? state : Join(localState.Value, state);
            }
            target[local] = localState ?? AssignementState.DefinitelyUnassigned;
        }
    }

    public override void Transfer(ref Dictionary<LocalSymbol, AssignementState> state, BoundNode node)
    {
        if (node is not BoundAssignmentExpression { Left: BoundLocalLvalue localLvalue }) return;

        state[localLvalue.Local] = AssignementState.DefinitelyAssigned;
    }

    private static AssignementState Join(AssignementState s1, AssignementState s2) => (s1, s2) switch
    {
        (AssignementState.DefinitelyAssigned, AssignementState.DefinitelyAssigned) => AssignementState.DefinitelyAssigned,
        (AssignementState.DefinitelyUnassigned, AssignementState.DefinitelyUnassigned) => AssignementState.DefinitelyUnassigned,
        _ => AssignementState.MaybeAssigned,
    };
}
