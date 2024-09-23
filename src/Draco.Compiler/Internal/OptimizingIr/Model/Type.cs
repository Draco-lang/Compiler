using System.Collections.Generic;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;
internal class Type(Module declaringModule, TypeSymbol symbol) : IType
{

    public TypeSymbol Symbol { get; } = symbol;
    public string Name => this.Symbol.Name;

    public Module DeclaringModule { get; } = declaringModule;
    IModule IType.DeclaringModule => this.DeclaringModule;
    public Assembly Assembly => this.DeclaringModule.Assembly;
    IAssembly IType.Assembly => this.Assembly;

    public IReadOnlyList<TypeParameterSymbol> Generics => this.Symbol.GenericParameters;
    public IReadOnlyDictionary<FunctionSymbol, IProcedure> Methods => this.methods;
    public IReadOnlyDictionary<FieldSymbol, IField> Fields => this.fields;

    private readonly Dictionary<FunctionSymbol, IProcedure> methods = [];
    private readonly Dictionary<FieldSymbol, IField> fields = [];

    public Procedure DefineMethod(FunctionSymbol functionSymbol)
    {
        if (!this.methods.TryGetValue(functionSymbol, out var result))
        {
            result = new Procedure(this.DeclaringModule, this, functionSymbol);
            this.methods.Add(functionSymbol, result);
        }
        return (Procedure)result;
    }

    public Field DefineField(FieldSymbol fieldSymbol)
    {
        if (!this.fields.TryGetValue(fieldSymbol, out var result))
        {
            result = new Field(fieldSymbol, this);
            this.fields.Add(fieldSymbol, result);
        }
        return (Field)result;
    }
}
