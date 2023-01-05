using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Semantics.FlowAnalysis;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Utility to represent <see cref="FlowOperation"/>s as a <see cref="IControlFlowGraph{TStatement}"/>.
/// </summary>
internal static class FlowOperationsToCfg
{
    private readonly record struct CfgAdapter(BasicBlock Entry) : IControlFlowGraph<FlowOperation>
    {
        IBasicBlock<FlowOperation> IControlFlowGraph<FlowOperation>.Entry => new BasicBlockAdapter(this.Entry);
        public IEnumerable<IBasicBlock<FlowOperation>> Exit => this.InternalBlocks
            .Where(b => b.Control is FlowControlOperation.Return)
            .Select(b => new BasicBlockAdapter(b))
            .Cast<IBasicBlock<FlowOperation>>();
        public IEnumerable<IBasicBlock<FlowOperation>> Blocks => this.InternalBlocks
            .Select(b => new BasicBlockAdapter(b))
            .Cast<IBasicBlock<FlowOperation>>();

        private IEnumerable<BasicBlock> InternalBlocks => GraphTraversal.DepthFirst(
            start: this.Entry,
            getNeighbors: bb => bb.Successors);
    }

    private readonly record struct BasicBlockAdapter(BasicBlock Block) : IBasicBlock<FlowOperation>
    {
        // TODO: Ugly cast...
        public IReadOnlyList<FlowOperation> Statements => (IReadOnlyList<FlowOperation>)this.Block.Operations;
        // TODO: Implement
        public IEnumerable<IBasicBlock<FlowOperation>> Predecessors => throw new NotImplementedException();
        public IEnumerable<IBasicBlock<FlowOperation>> Successors => this.Block.Successors
            .Select(b => new BasicBlockAdapter(b))
            .Cast<IBasicBlock<FlowOperation>>();
    }

    public static IControlFlowGraph<FlowOperation> ToCfg(BasicBlock entry) => new CfgAdapter(entry);
}
