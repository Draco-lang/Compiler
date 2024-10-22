using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis.Domains;

/// <summary>
/// The possible states of a variable being assigned.
/// </summary>
internal enum AssignmentState
{
    /// <summary>
    /// The status is unknown.
    /// </summary>
    Unknown,

    /// <summary>
    /// The variable is definitely unassigned.
    /// </summary>
    Unassigned,

    /// <summary>
    /// The variable is definitely assigned.
    /// </summary>
    Assigned,
}

/// <summary>
/// A domain for performing definite assignment analysis.
/// Essentially checks, if a local variable is definitely assigned at a given point in the program.
/// </summary>
internal sealed class DefiniteAssignmentDomain(IEnumerable<LocalSymbol> locals)
    : FlowDomain<Dictionary<LocalSymbol, AssignmentState>>
{
    /// <summary>
    /// The locals being tracked by the domain.
    /// </summary>
    public ImmutableArray<LocalSymbol> Locals { get; } = locals.ToImmutableArray();

    public override FlowDirection Direction => FlowDirection.Forward;
    public override Dictionary<LocalSymbol, AssignmentState> Initial =>
        this.Locals.ToDictionary(l => l, l => IsForLoopVariable(l) ? AssignmentState.Assigned : AssignmentState.Unassigned);
    public override Dictionary<LocalSymbol, AssignmentState> Top =>
        this.Locals.ToDictionary(l => l, l => IsForLoopVariable(l) ? AssignmentState.Assigned : AssignmentState.Unknown);

    public override Dictionary<LocalSymbol, AssignmentState> Clone(in Dictionary<LocalSymbol, AssignmentState> state) => new(state);

    public override string ToString(Dictionary<LocalSymbol, AssignmentState> state) =>
        $"[{string.Join(", ", state.OrderBy(kv => kv.Key.Name).Select(kv => $"{kv.Key.Name}: {kv.Value}"))}]";

    public override bool Equals(Dictionary<LocalSymbol, AssignmentState> state1, Dictionary<LocalSymbol, AssignmentState> state2)
    {
        if (state1.Count != state2.Count) return false;
        foreach (var (key, value) in state1)
        {
            if (!state2.TryGetValue(key, out var otherValue)) return false;
            if (value != otherValue) return false;
        }
        return true;
    }

    public override void Join(
        ref Dictionary<LocalSymbol, AssignmentState> target,
        IEnumerable<Dictionary<LocalSymbol, AssignmentState>> sources)
    {
        // First off we clear out target to unknown
        foreach (var local in this.Locals) target[local] = AssignmentState.Unknown;

        // Now we need to go through each local and see what each source says
        foreach (var local in this.Locals)
        {
            var state = AssignmentState.Unknown;
            foreach (var source in sources)
            {
                if (!source.TryGetValue(local, out var sourceState)) continue;
                Join(ref state, sourceState);
            }
            target[local] = state;
        }
    }

    public override void Transfer(ref Dictionary<LocalSymbol, AssignmentState> state, BoundNode node)
    {
        if (node is not BoundAssignmentExpression { Left: BoundLocalLvalue local }) return;

        state[local.Local] = AssignmentState.Assigned;
    }

    private static void Join(ref AssignmentState target, AssignmentState source)
    {
        // If any of the sources are unassigned, then the target is unassigned
        if (source == AssignmentState.Unassigned)
        {
            target = AssignmentState.Unassigned;
            return;
        }
        // If target is still unknown, we can update unconditionally
        if (target == AssignmentState.Unknown)
        {
            target = source;
            return;
        }
    }

    // TODO: Is this too hacky? Should we instead add a flag to the local symbol?
    private static bool IsForLoopVariable(LocalSymbol local) =>
        local.DeclaringSyntax?.Parent is ForExpressionSyntax;
}
