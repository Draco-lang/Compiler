using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal sealed class Module : IModule
{
    private static readonly string doubleNewline = $"{Environment.NewLine}{Environment.NewLine}";

    public ModuleSymbol Symbol { get; }

    public string Name => this.Symbol.Name;

    public IReadOnlyDictionary<ModuleSymbol, IModule> Submodules => this.submodules;
    public IReadOnlyDictionary<TypeSymbol, IClass> Classes => this.classes;

    public IReadOnlySet<GlobalSymbol> Globals => this.globals;

    public Procedure GlobalInitializer { get; }
    IProcedure IModule.GlobalInitializer => this.GlobalInitializer;

    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;

    public Assembly Assembly { get; }
    IAssembly IModule.Assembly => this.Assembly;

    public Module? Parent { get; }
    IModule? IModule.Parent => this.Parent;

    private readonly HashSet<GlobalSymbol> globals = new();
    private readonly Dictionary<TypeSymbol, IClass> classes = new();
    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = new();
    private readonly Dictionary<ModuleSymbol, IModule> submodules = new();

    public Module(ModuleSymbol symbol, Assembly assembly, Module? Parent)
    {
        this.Symbol = symbol;
        this.GlobalInitializer = this.DefineProcedure(new IntrinsicFunctionSymbol(
            name: ".cctor",
            paramTypes: Enumerable.Empty<TypeSymbol>(),
            returnType: IntrinsicSymbols.Unit));
        this.Assembly = assembly;
        this.Parent = Parent;
    }

    public ImmutableArray<IProcedure> GetProcedures()
    {
        var result = ImmutableArray.CreateBuilder<IProcedure>();
        result.AddRange(this.procedures.Values.ToImmutableArray());
        foreach (var submodule in this.submodules.Values)
        {
            result.AddRange(((Module)submodule).GetProcedures());
        }
        return result.ToImmutable();
    }

    public void DefineGlobal(GlobalSymbol globalSymbol) => this.globals.Add(globalSymbol);

    public Procedure DefineProcedure(FunctionSymbol functionSymbol)
    {
        if (!this.procedures.TryGetValue(functionSymbol, out var result))
        {
            result = new Procedure(this, functionSymbol);
            this.procedures.Add(functionSymbol, result);
        }
        return (Procedure)result;
    }

    public Class DefineClass(TypeSymbol typeSymbol)
    {
        if (!this.classes.TryGetValue(typeSymbol, out var result))
        {
            result = new Class(this, null, typeSymbol);
            this.classes.Add(typeSymbol, result);
        }
        return (Class)result;
    }

    public Module DefineModule(ModuleSymbol moduleSymbol)
    {
        if (!this.submodules.TryGetValue(moduleSymbol, out var result))
        {
            result = new Module(moduleSymbol, this.Assembly, this);
            this.submodules.Add(moduleSymbol, result);
        }
        return (Module)result;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"module {this.Symbol.Name} {{");
        result.AppendJoin(Environment.NewLine, this.globals);

        var haveNewline = this.globals.Count == 0;
        void PrintComponents(IEnumerable<object> components)
        {
            if (!components.Any()) return;
            if (!haveNewline) result!.Append(doubleNewline);
            result!.AppendJoin(doubleNewline, components);
            haveNewline = false;
        }

        PrintComponents(this.procedures.Values);
        PrintComponents(this.classes.Values);
        PrintComponents(this.submodules.Values);

        if (!haveNewline) result.AppendLine();
        result.Append('}');
        return result.ToString();
    }
}
