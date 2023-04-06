using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Internal.FlowAnalysis.ReturnsOnAllPaths;

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
        var state = pass.Analyze(node);
        // TODO
        throw new NotImplementedException();
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
        NotInitialized,
        Initialized,
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
            if (target.Locals.TryGetValue(local, out var existingStatus) && (int)existingStatus >= (int)status) continue;
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
            if (target.Locals.TryGetValue(local, out var existingStatus) && (int)existingStatus <= (int)status) continue;
            target.Locals[local] = status;
            changed = true;
        }
        return changed;
    }

    private readonly List<LocalSymbol> locals;

    public DefiniteAssignment(List<LocalSymbol> locals)
    {
        this.locals = locals;
    }
}
