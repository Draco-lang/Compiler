using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IProcedure"/> implementation.
/// </summary>
internal sealed class Procedure : IProcedure
{
    public FunctionSymbol Symbol { get; }
    public Assembly Assembly { get; }
    IAssembly IProcedure.Assembly => this.Assembly;
    public BasicBlock Entry { get; }
    IBasicBlock IProcedure.Entry => this.Entry;
    public IEnumerable<BasicBlock> BasicBlocks => GraphTraversal.DepthFirst(
        start: this.Entry,
        getNeighbors: bb => bb.Successors);
    IEnumerable<IBasicBlock> IProcedure.BasicBlocks => this.BasicBlocks;

    public Procedure(Assembly assembly, FunctionSymbol symbol)
    {
        this.Assembly = assembly;
        this.Symbol = symbol;
        this.Entry = new(this);
    }

    public BasicBlock DefineBasicBlock() => new(this);
}
