using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.OptimizingIr.Model;
internal class Class(Module declaringModule, TypeSymbol symbol) : IClass
{

    public TypeSymbol Symbol { get; } = symbol;
    public string Name => this.Symbol.Name;

    public Module DeclaringModule { get; } = declaringModule;
    IModule IClass.DeclaringModule => this.DeclaringModule;
    public Assembly Assembly => this.DeclaringModule.Assembly;
    IAssembly IClass.Assembly => this.Assembly;

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
