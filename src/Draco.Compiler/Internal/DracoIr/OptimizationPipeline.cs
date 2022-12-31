using Draco.Compiler.Internal.DracoIr.Passes;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Builds up the whole optimization pipeline.
/// </summary>
internal static class OptimizationPipeline
{
    public static IOptimizationPass Instance { get; } = OptimizationPass.Fixpoint(OptimizationPass.Sequence(
        OptimizationPass.Fixpoint(JumpThreading.Instance),
        DeadBlockElimination.Instance,
        //InlineSmallBlocks.Instance,
        TailCallOptimization.Instance));
}
