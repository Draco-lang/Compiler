using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.DracoIr.Passes;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Builds up the whole optimization pipeline.
/// </summary>
internal static class OptimizationPipeline
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Sequence(
        OptimizationPass.Fixpoint(JumpThreading.Instance),
        DeadBlockElimination.Instance);
}
