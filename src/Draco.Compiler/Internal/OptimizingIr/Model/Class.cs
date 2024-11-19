using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

internal sealed class Class(TypeSymbol symbol) : IClass
{
    public TypeSymbol Symbol { get; } = symbol;
    public string Name => this.Symbol.Name;

    public IReadOnlyList<TypeParameterSymbol> Generics => this.Symbol.GenericParameters;
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Procedures => this.procedures;

    public IReadOnlySet<FieldSymbol> Fields => this.fields;
    public IReadOnlySet<PropertySymbol> Properties => this.properties;

    private readonly HashSet<FieldSymbol> fields = [];
    private readonly HashSet<PropertySymbol> properties = [];
    private readonly Dictionary<FunctionSymbol, IProcedure> procedures = [];

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
}
