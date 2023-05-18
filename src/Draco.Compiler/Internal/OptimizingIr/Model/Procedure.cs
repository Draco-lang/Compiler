using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IProcedure"/> implementation.
/// </summary>
internal sealed class Procedure : IProcedure
{
    public FunctionSymbol Symbol { get; }
    public string Name => this.Symbol.Name;
    public TypeSymbol? Type => this.Symbol.Type;
    public Module DeclaringModule { get; }
    IModule IProcedure.DeclaringModule => this.DeclaringModule;
    public Assembly Assembly => this.DeclaringModule.Assembly;
    IAssembly IProcedure.Assembly => this.Assembly;
    public BasicBlock Entry { get; }
    IBasicBlock IProcedure.Entry => this.Entry;
    public IReadOnlyDictionary<ParameterSymbol, Parameter> Parameters => this.parameters;
    public IEnumerable<Parameter> ParametersInDefinitionOrder => this.parameters.Values.OrderBy(p => p.Index);
    public TypeSymbol ReturnType => this.Symbol.ReturnType;
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks => this.basicBlocks;
    public IEnumerable<IBasicBlock> BasicBlocksInDefinitionOrder => this.basicBlocks.Values
        .Cast<BasicBlock>()
        .OrderBy(bb => bb.Index);
    public IReadOnlyDictionary<LocalSymbol, Local> Locals => this.locals;
    public IEnumerable<Local> LocalsInDefinitionOrder => this.locals.Values.OrderBy(l => l.Index);
    public IReadOnlyList<Register> Registers => this.registers;

    private readonly Dictionary<ParameterSymbol, Parameter> parameters = new();
    private readonly Dictionary<LabelSymbol, IBasicBlock> basicBlocks = new();
    private readonly Dictionary<LocalSymbol, Local> locals = new();
    private readonly List<Register> registers = new();

    public Procedure(Module declaringModule, FunctionSymbol symbol)
    {
        this.DeclaringModule = declaringModule;
        this.Symbol = symbol;
        this.Entry = this.DefineBasicBlock(new SynthetizedLabelSymbol("begin"));
    }

    public Parameter DefineParameter(ParameterSymbol symbol)
    {
        if (!this.parameters.TryGetValue(symbol, out var param))
        {
            param = new Parameter(symbol, this.parameters.Count);
            this.parameters.Add(symbol, param);
        }
        return param;
    }

    public BasicBlock DefineBasicBlock(LabelSymbol symbol)
    {
        if (!this.basicBlocks.TryGetValue(symbol, out var block))
        {
            block = new BasicBlock(this, symbol);
            this.basicBlocks.Add(symbol, block);
        }
        return (BasicBlock)block;
    }

    public bool RemoveBasicBlock(IBasicBlock bb) => this.basicBlocks.Remove(bb.Symbol);

    public Local DefineLocal(LocalSymbol symbol)
    {
        if (!this.locals.TryGetValue(symbol, out var result))
        {
            result = new Local(symbol, this.locals.Count);
            this.locals.Add(symbol, result);
        }
        return result;
    }

    public Register DefineRegister(TypeSymbol type)
    {
        var result = new Register(type, this.registers.Count);
        this.registers.Add(result);
        return result;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append($"proc {this.ToOperandString()}(");
        result.AppendJoin(", ", this.ParametersInDefinitionOrder);
        result.AppendLine($") {this.ReturnType}:");
        if (this.Locals.Count > 0)
        {
            result.AppendLine("locals:");
            foreach (var local in this.LocalsInDefinitionOrder) result.AppendLine($"  {local}");
        }
        result.AppendJoin(System.Environment.NewLine, this.BasicBlocksInDefinitionOrder);
        return result.ToString();
    }

    public string ToOperandString() => this.Symbol.Name;
}
