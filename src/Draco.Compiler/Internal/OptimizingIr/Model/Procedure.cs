using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
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
    public IReadOnlyDictionary<ParameterSymbol, Parameter> Parameters => this.parameters;
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks => this.basicBlocks;
    public IReadOnlyDictionary<LocalSymbol, Local> Locals => this.locals;

    private readonly Dictionary<ParameterSymbol, Parameter> parameters = new();
    private readonly Dictionary<LabelSymbol, IBasicBlock> basicBlocks = new();
    private readonly Dictionary<LocalSymbol, Local> locals = new();
    private int basicBlockIndex = 0;
    private int localIndex = 0;
    private int registerIndex = 0;

    public Procedure(Assembly assembly, FunctionSymbol symbol)
    {
        this.Assembly = assembly;
        this.Symbol = symbol;
        this.Entry = this.DefineBasicBlock(new SynthetizedLabelSymbol("begin"));
    }

    public Parameter DefineParameter(ParameterSymbol symbol)
    {
        if (!this.parameters.TryGetValue(symbol, out var param))
        {
            param = new Parameter(symbol);
            this.parameters.Add(symbol, param);
        }
        return param;
    }

    public BasicBlock DefineBasicBlock(LabelSymbol symbol)
    {
        if (!this.basicBlocks.TryGetValue(symbol, out var block))
        {
            block = new BasicBlock(this, symbol, this.basicBlockIndex++);
            this.basicBlocks.Add(symbol, block);
        }
        return (BasicBlock)block;
    }

    public Local DefineLocal(LocalSymbol symbol)
    {
        if (!this.locals.TryGetValue(symbol, out var result))
        {
            result = new Local(symbol, this.localIndex++);
            this.locals.Add(symbol, result);
        }
        return result;
    }

    public Register DefineRegister() => new(this.registerIndex++);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append($"proc {this.ToOperandString()}(");
        result.AppendJoin(", ", this.Parameters.Values);
        result.AppendLine("):");
        if (this.Locals.Count > 0)
        {
            result.AppendLine("locals:");
            foreach (var local in this.locals.Values) result.AppendLine($"  {local}");
        }
        var blocksInOrder = this.BasicBlocks.Values
            .Cast<BasicBlock>()
            .OrderBy(bb => bb.Index);
        result.AppendJoin(System.Environment.NewLine, blocksInOrder);
        return result.ToString();
    }

    public string ToOperandString() => this.Symbol.Name;
}
