using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    public IReadOnlyList<FieldSymbol> Fields => InterlockedUtils.InitializeDefault(
        ref this.fields,
        () => this.Symbol.DefinedMembers.OfType<FieldSymbol>().ToImmutableArray());
    private ImmutableArray<FieldSymbol> fields;

    private readonly Dictionary<FunctionSymbol, IProcedure> methods = [];

    public Procedure DefineMethod(FunctionSymbol functionSymbol)
    {
        if (!this.methods.TryGetValue(functionSymbol, out var result))
        {
            result = new Procedure(this.DeclaringModule, this, functionSymbol);
            this.methods.Add(functionSymbol, result);
        }
        return (Procedure)result;
    }
}
