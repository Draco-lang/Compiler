using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IProcedure"/> implementation.
/// </summary>
internal sealed class Procedure : IProcedure
{
    public FunctionSymbol Symbol { get; }
    public string Name => this.Symbol.NestedName;
    public BasicBlock Entry { get; }
    IBasicBlock IProcedure.Entry => this.Entry;
    public IReadOnlyList<AttributeInstance> Attributes => this.Symbol.Attributes;
    public IReadOnlyList<TypeParameterSymbol> Generics => this.Symbol.GenericParameters;
    public IReadOnlyList<ParameterSymbol> Parameters => this.Symbol.Parameters;
    public TypeSymbol ReturnType => this.Symbol.ReturnType;
    public IReadOnlyDictionary<LabelSymbol, IBasicBlock> BasicBlocks => this.basicBlocks;
    public IEnumerable<IBasicBlock> BasicBlocksInDefinitionOrder => this.basicBlocks.Values
        .Cast<BasicBlock>()
        .OrderBy(bb => bb.Index);
    public IReadOnlyList<LocalSymbol> Locals => this.locals;
    public IReadOnlyList<Register> Registers => this.registers;

    public bool IsStatic => this.Symbol.IsStatic;

    private readonly Dictionary<LabelSymbol, IBasicBlock> basicBlocks = [];
    private readonly List<LocalSymbol> locals = [];
    private readonly List<Register> registers = [];

    public Procedure(FunctionSymbol symbol)
    {
        this.Symbol = symbol;
        this.Entry = this.DefineBasicBlock(new SynthetizedLabelSymbol("begin"));
    }

    public int GetParameterIndex(ParameterSymbol symbol)
    {
        if (symbol.IsThis) throw new ArgumentOutOfRangeException(nameof(symbol), "this parameter is treated special");
        for (var i = 0; i < this.Parameters.Count; ++i)
        {
            var param = this.Parameters[i];
            // TODO: A little janky...
            if (param == symbol || param.GenericDefinition == symbol) return i;
        }
        throw new ArgumentOutOfRangeException(nameof(symbol));
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

    public int DefineLocal(LocalSymbol symbol)
    {
        var index = this.locals.IndexOf(symbol);
        if (index == -1)
        {
            index = this.locals.Count;
            this.locals.Add(symbol);
        }
        return index;
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
        result.Append($"proc {this.Name}");
        if (this.Generics.Count > 0)
        {
            result.Append('<');
            result.AppendJoin(", ", this.Generics);
            result.Append('>');
        }
        result.Append('(');
        result.AppendJoin(", ", this.Parameters);
        result.AppendLine($") {this.ReturnType}:");
        if (this.Locals.Count > 0)
        {
            result.AppendLine("locals:");
            foreach (var local in this.Locals) result.AppendLine($"  {local}");
        }
        result.AppendJoin(System.Environment.NewLine, this.BasicBlocksInDefinitionOrder);
        return result.ToString();
    }
}
