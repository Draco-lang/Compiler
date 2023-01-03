using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.FlowAnalysis;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Utility to represent Draco IR as a <see cref="IControlFlowGraph{TStatement}"/>.
/// </summary>
internal static class DracoIrToCfg
{
    private readonly record struct CfgAdapter(IReadOnlyProcedure Procedure) : IControlFlowGraph<IReadOnlyInstruction>
    {
        public IBasicBlock<IReadOnlyInstruction> Entry => new BasicBlockAdapter(this.Procedure.Entry);
        public IEnumerable<IBasicBlock<IReadOnlyInstruction>> Exit => this.Procedure.BasicBlocks
            .Where(bb => bb.Instructions[^1].Kind == InstructionKind.Ret)
            .Select(b => new BasicBlockAdapter(b))
            .Cast<IBasicBlock<IReadOnlyInstruction>>();
        public IEnumerable<IBasicBlock<IReadOnlyInstruction>> Blocks => this.Procedure.BasicBlocks
            .Select(b => new BasicBlockAdapter(b))
            .Cast<IBasicBlock<IReadOnlyInstruction>>();
    }

    private readonly record struct BasicBlockAdapter(IReadOnlyBasicBlock BasicBlock) : IBasicBlock<IReadOnlyInstruction>
    {
        public IReadOnlyList<IReadOnlyInstruction> Statements => this.BasicBlock.Instructions;
        public IEnumerable<IBasicBlock<IReadOnlyInstruction>> Predecessors
        {
            get
            {
                var block = this.BasicBlock;
                return this.BasicBlock.Procedure.BasicBlocks
                    .Where(bb => new BasicBlockAdapter(bb).Successors.Any(s => ((BasicBlockAdapter)s).BasicBlock == block))
                    .Cast<IBasicBlock<IReadOnlyInstruction>>();
            }
        }
        public IEnumerable<IBasicBlock<IReadOnlyInstruction>> Successors
        {
            get
            {
                var lastInstr = this.BasicBlock.Instructions[^1];
                for (var i = 0; i < lastInstr.OperandCount; ++i)
                {
                    if (lastInstr[i] is IReadOnlyBasicBlock bb) yield return new BasicBlockAdapter(bb);
                }
            }
        }
    }

    /// <summary>
    /// Converts the given IR procedure to a control-flow graph.
    /// </summary>
    /// <param name="procedure">The IR procedure to convert.</param>
    /// <returns>The CFG for <paramref name="procedure"/>.</returns>
    public static IControlFlowGraph<IReadOnlyInstruction> ToCfg(IReadOnlyProcedure procedure) => new CfgAdapter(procedure);
}
