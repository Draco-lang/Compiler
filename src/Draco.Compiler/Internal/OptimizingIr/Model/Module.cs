using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal sealed class Module : IModule
{
    private static readonly string doubleNewline = $"{Environment.NewLine}{Environment.NewLine}";

    public ModuleSymbol Symbol { get; }

    public string Name => this.Symbol.Name;

    public IDictionary<ModuleSymbol, IModule> Submodules => this.submodules;
    IReadOnlyDictionary<ModuleSymbol, IModule> IModule.Submodules => this.submodules;

    public IDictionary<TypeSymbol, IClass> Classes => this.classes;
    IReadOnlyDictionary<TypeSymbol, IClass> IModule.Classes => this.classes;

    public IReadOnlySet<FieldSymbol> Fields => this.fields;
    public IReadOnlySet<PropertySymbol> Properties => this.properties;

    public Procedure GlobalInitializer { get; }
    IProcedure IModule.GlobalInitializer => this.GlobalInitializer;

    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;
    IReadOnlyDictionary<FunctionSymbol, IProcedure> IModule.Procedures => this.procedures;

    private readonly HashSet<FieldSymbol> fields = [];
    private readonly HashSet<PropertySymbol> properties = [];
    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = [];
    private readonly Dictionary<ModuleSymbol, IModule> submodules = [];
    private readonly Dictionary<TypeSymbol, IClass> classes = [];

    public Module(ModuleSymbol symbol)
    {
        this.Symbol = symbol;
        this.GlobalInitializer = this.DefineProcedure(new IntrinsicFunctionSymbol(
            name: "<global initializer>",
            paramTypes: [],
            returnType: WellKnownTypes.Unit));
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

    public void DefineField(FieldSymbol fieldSymbol) => this.fields.Add(fieldSymbol);
    public void DefineProperty(PropertySymbol propertySymbol) => this.properties.Add(propertySymbol);

    public Procedure DefineProcedure(FunctionSymbol functionSymbol)
    {
        if (!this.procedures.TryGetValue(functionSymbol, out var result))
        {
            result = new Procedure(functionSymbol);
            this.procedures.Add(functionSymbol, result);
        }
        return (Procedure)result;
    }

    public Module DefineModule(ModuleSymbol moduleSymbol)
    {
        if (!this.submodules.TryGetValue(moduleSymbol, out var result))
        {
            result = new Module(moduleSymbol);
            this.submodules.Add(moduleSymbol, result);
        }
        return (Module)result;
    }

    public Class DefineClass(TypeSymbol typeSymbol)
    {
        if (!this.classes.TryGetValue(typeSymbol, out var result))
        {
            result = new Class(typeSymbol);
            this.classes.Add(typeSymbol, result);
        }
        return (Class)result;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"module {this.Symbol.Name}");
        result.AppendJoin(Environment.NewLine, this.fields);
        if (this.fields.Count > 0 && this.procedures.Count > 1) result.Append(doubleNewline);
        result.AppendJoin(doubleNewline, this.procedures.Values);
        if (this.procedures.Count > 0 && this.submodules.Count > 0) result.Append(doubleNewline);
        result.AppendJoin(doubleNewline, this.submodules.Values);
        return result.ToString();
    }
}
