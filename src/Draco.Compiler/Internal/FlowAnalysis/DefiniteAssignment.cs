using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
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
        // TODO
        throw new NotImplementedException();
    }

    public enum AssignmentStatus
    {
        NotInitialized,
        Initialized,
    }

    public readonly record struct LocalState(Dictionary<LocalSymbol, AssignmentStatus> Locals);

    public override LocalState Top => new(Locals: new());
    public override LocalState Bottom => throw new NotImplementedException();

    public override LocalState Clone(in LocalState state) => throw new NotImplementedException();
    public override bool Join(ref LocalState target, in LocalState other) => throw new NotImplementedException();
    public override bool Meet(ref LocalState target, in LocalState other) => throw new NotImplementedException();
}
