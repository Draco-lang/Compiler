using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Passes;
using static Draco.Compiler.Internal.OptimizingIr.PassFactory;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Builds up the whole optimization pipeline.
/// </summary>
internal static class OptimizationPipeline
{
    public static IPass Instance { get; } = Fixpoint(Sequence(
        Fixpoint(JumpThreading.Instance),
        DeadBlockElimination.Instance));
}
