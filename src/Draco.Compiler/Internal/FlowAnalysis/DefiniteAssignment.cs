using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Checks, if all variable reads happen after initialization.
/// https://en.wikipedia.org/wiki/Definite_assignment_analysis
/// </summary>
internal sealed class DefiniteAssignment : FlowAnalysisPass<DefiniteAssignment.LocalState>
{
    public static void Analyze(BoundNode node, DiagnosticBag diagnostics)
    {
        var locals = LocalCollector.Collect(node);
        var pass = new DefiniteAssignment(locals);
        _ = pass.Analyze(node);

        foreach (var (reference, status) in pass.referenceStates)
        {
            if (status != AssignmentStatus.NotInitialized) continue;

            diagnostics.Add(Diagnostic.Create(
                template: FlowAnalysisErrors.VariableUsedBeforeInit,
                location: reference.Syntax?.Location,
                formatArgs: reference.Local.Name));
        }
    }

    private sealed class LocalCollector : BoundTreeVisitor
    {
        public static List<LocalSymbol> Collect(BoundNode node)
        {
            var collector = new LocalCollector();
            node.Accept(collector);
            return collector.locals;
        }

        private readonly List<LocalSymbol> locals = new();

        public override void VisitLocalDeclaration(BoundLocalDeclaration node) =>
            this.locals.Add(node.Local);
    }

    public enum AssignmentStatus
    {
        NotInitialized = 0,
        Initialized = 1,
    }

    public readonly record struct LocalState(Dictionary<LocalSymbol, AssignmentStatus> Locals);

    public override LocalState Top => new(Locals: new());
    public override LocalState Bottom => new(Locals: this.locals.ToDictionary(s => s, _ => AssignmentStatus.Initialized));

    public override LocalState Clone(in LocalState state) => new(Locals: new(state.Locals));

    public override bool Join(ref LocalState target, in LocalState other)
    {
        var changed = false;
        foreach (var (local, status) in other.Locals)
        {
            if (target.Locals.TryGetValue(local, out var existingStatus) && (int)existingStatus <= (int)status) continue;
            target.Locals[local] = status;
            changed = true;
        }
        return changed;
    }

    public override bool Meet(ref LocalState target, in LocalState other)
    {
        var changed = false;
        foreach (var (local, status) in other.Locals)
        {
            if (target.Locals.TryGetValue(local, out var existingStatus) && (int)existingStatus >= (int)status) continue;
            target.Locals[local] = status;
            changed = true;
        }
        return changed;
    }

    private readonly List<LocalSymbol> locals;
    private readonly Dictionary<BoundLocalExpression, AssignmentStatus> referenceStates = new();

    public DefiniteAssignment(List<LocalSymbol> locals)
    {
        this.locals = locals;
    }

    public override void VisitLocalDeclaration(BoundLocalDeclaration node)
    {
        node.Value?.Accept(this);
        var status = node.Value is null ? AssignmentStatus.NotInitialized : AssignmentStatus.Initialized;
        this.State.Locals[node.Local] = status;
    }

    public override void VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        base.VisitAssignmentExpression(node);
        if (node.Left is not BoundLocalLvalue localLvalue) return;
        this.State.Locals[localLvalue.Local] = AssignmentStatus.Initialized;
    }

    public override void VisitLocalExpression(BoundLocalExpression node)
    {
        // We check referenced-ness status
        if (!this.State.Locals.TryGetValue(node.Local, out var status))
        {
            // NOTE: This assumption might become bad, once we start resolving locals out of order
            // It's probably an error, assume initialized to avoid cascading errors
            status = AssignmentStatus.Initialized;
        }
        this.referenceStates[node] = status;
    }
}
