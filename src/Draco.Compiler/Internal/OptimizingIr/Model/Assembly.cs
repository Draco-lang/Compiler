using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A mutable <see cref="IAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IAssembly
{
    private static readonly string doubleNewline = $"{Environment.NewLine}{Environment.NewLine}";

    public ModuleSymbol Symbol { get; }
    public string Name { get; set; } = "output";
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;

    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = new();

    public Assembly(ModuleSymbol symbol)
    {
        this.Symbol = symbol;
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

    public override string ToString() => string.Join(doubleNewline, this.procedures.Values);
}
