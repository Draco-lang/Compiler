using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;
internal sealed class Module : IModule
{
    private static readonly string doubleNewline = $"{Environment.NewLine}{Environment.NewLine}";

    private readonly Dictionary<GlobalSymbol, Global> globals = new();
    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = new();
    private readonly Dictionary<ModuleSymbol, IModule> subModules = new();

    public ModuleSymbol Symbol { get; }

    public string Name => this.Symbol.Name;

    public IReadOnlyDictionary<ModuleSymbol, IModule> SubModules => this.subModules;

    public IReadOnlyDictionary<GlobalSymbol, Global> Globals => this.globals;

    public Procedure GlobalInitializer { get; }
    IProcedure IModule.GlobalInitializer => this.GlobalInitializer;

    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;

    public Assembly Assembly { get; }
    IAssembly IModule.Assembly => this.Assembly;

    public Module(ModuleSymbol symbol, Assembly assembly)
    {
        this.Symbol = symbol;
        this.GlobalInitializer = this.DefineProcedure(new IntrinsicFunctionSymbol(
            name: "<global initializer>",
            paramTypes: Enumerable.Empty<TypeSymbol>(),
            returnType: IntrinsicSymbols.Unit));
        this.Assembly = assembly;
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

    public Module DefineModule(ModuleSymbol moduleSymbol)
    {
        if (!this.subModules.TryGetValue(moduleSymbol, out var result))
        {
            result = new Module(moduleSymbol, this.Assembly);
            this.subModules.Add(moduleSymbol, result);
        }
        return (Module)result;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine(this.Symbol.Name);
        result.AppendJoin(Environment.NewLine, this.globals.Values);
        if (this.globals.Count > 0 && this.procedures.Count > 1) result.Append(doubleNewline);
        result.AppendJoin(doubleNewline, this.procedures.Values);
        if (this.procedures.Count > 0 && this.subModules.Count > 1) result.Append(doubleNewline);
        result.AppendJoin(doubleNewline, this.subModules.Values);
        return result.ToString();
    }
}
