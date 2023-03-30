using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
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
    public IReadOnlyCollection<Local> Locals => this.locals;

    private readonly List<Local> locals = new();
    private int basicBlockIndex = 0;
    private int registerIndex = 0;

    public Procedure(Assembly assembly, FunctionSymbol symbol)
    {
        this.Assembly = assembly;
        this.Symbol = symbol;
        this.Entry = this.DefineBasicBlock();
    }

    public BasicBlock DefineBasicBlock() => new(this, this.basicBlockIndex++);
    public Local DefineLocal(LocalSymbol symbol)
    {
        var result = new Local(symbol, this.locals.Count);
        this.locals.Add(result);
        return result;
    }
    public Local DefineLocal(Type type)
    {
        var result = new Local(type, this.locals.Count);
        this.locals.Add(result);
        return result;
    }
    public Register DefineRegister() => new(this.registerIndex++);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"proc {this.ToOperandString()}():");
        if (this.Locals.Count > 0)
        {
            result.AppendLine("locals (");
            foreach (var local in this.locals) result.AppendLine($"  {local}");
            result.AppendLine(")");
        }
        var blocksInOrder = this.BasicBlocks
            .OrderBy(bb => bb.Index);
        result.AppendJoin(System.Environment.NewLine, blocksInOrder);
        return result.ToString();
    }

    public string ToOperandString() => this.Symbol.Name;
}
