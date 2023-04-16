using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IAssembly
{
    private static readonly string doubleNewline = $"{System.Environment.NewLine}{System.Environment.NewLine}";

    public ModuleSymbol Symbol { get; }
    public string Name { get; set; } = "output";
    public IReadOnlyDictionary<GlobalSymbol, Global> Globals => this.globals;
    public Procedure GlobalInitializer { get; }
    IProcedure IAssembly.GlobalInitializer => this.GlobalInitializer;
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;
    public Procedure? EntryPoint
    {
        get => this.entryPoint;
        set
        {
            if (value is null)
            {
                this.entryPoint = null;
                return;
            }
            if (!ReferenceEquals(this, value.Assembly))
            {
                throw new System.InvalidOperationException("entry point must be part of the assembly");
            }
            this.entryPoint = value;
        }
    }
    IProcedure? IAssembly.EntryPoint => this.EntryPoint;

    private readonly Dictionary<GlobalSymbol, Global> globals = new();
    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = new();
    private Procedure? entryPoint;

    public Assembly(ModuleSymbol symbol)
    {
        this.Symbol = symbol;
        this.GlobalInitializer = this.DefineProcedure(new IntrinsicFunctionSymbol(
            name: "<global initializer>",
            paramTypes: Enumerable.Empty<TypeSymbol>(),
            returnType: IntrinsicSymbols.Unit));
    }

    public Global DefineGlobal(GlobalSymbol globalSymbol)
    {
        if (!this.globals.TryGetValue(globalSymbol, out var result))
        {
            result = new Global(globalSymbol);
            this.globals.Add(globalSymbol, result);
        }
        return result;
    }

    public Procedure DefineProcedure(FunctionSymbol functionSymbol)
    {
        if (!this.procedures.TryGetValue(functionSymbol, out var result))
        {
            result = new Procedure(this, functionSymbol);
            this.procedures.Add(functionSymbol, result);
        }
        return (Procedure)result;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"assembly {this.Name}");
        if (this.EntryPoint is not null) result.AppendLine($"entry {this.EntryPoint.Name}");
        result.AppendLine();
        result.AppendJoin(System.Environment.NewLine, this.globals.Values);
        if (this.globals.Count > 0 && this.procedures.Count > 1) result.Append(doubleNewline);
        result.AppendJoin(doubleNewline, this.procedures.Values);
        return result.ToString();
    }
}
