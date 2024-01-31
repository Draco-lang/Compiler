using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Internal.Symbols.Generic;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A constructor function for types.
/// </summary>
internal sealed class ConstructorFunctionSymbol : FunctionSymbol
{
    public override string Name => this.InstantiatedType.Name;
    public override bool IsSpecialName => true;
    public override Api.Semantics.Visibility Visibility => this.ctorDefinition.Visibility;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => InterlockedUtils.InitializeNull(ref this.returnType, this.BuildReturnType);
    private TypeSymbol? returnType;

    private FunctionSymbol ConstructorSymbol => InterlockedUtils.InitializeNull(ref this.constructorSymbol, this.BuildConstructorSymbol);
    private FunctionSymbol? constructorSymbol;

    public override CodegenDelegate Codegen => (codegen, target, args) =>
    {
        var instance = target.Type;
        var ctorSymbol = this.ConstructorSymbol;
        if (instance.IsGenericInstance && this.IsGenericDefinition)
        {
            ctorSymbol = ctorSymbol.GenericInstantiate(instance, ((IGenericInstanceSymbol)instance).Context);
        }
        codegen.Write(NewObject(target, ctorSymbol, args));
    };

    private GenericContext? Context
    {
        get
        {
            if (!this.contextNeedsBuild) return this.context;
            lock (this.contextBuildLock)
            {
                if (this.contextNeedsBuild)
                {
                    this.context = this.BuildContext();
                    // IMPORTANT: We write the flag last and here
                    this.contextNeedsBuild = false;
                }
                return this.context;
            }
        }
    }
    private GenericContext? context;
    // IMPORTANT: Flag is a bool and not computed because we can't atomically copy nullable structs
    private volatile bool contextNeedsBuild = true;
    private readonly object contextBuildLock = new();

    private TypeSymbol InstantiatedType => (TypeSymbol)this.ctorDefinition.ContainingSymbol!;

    private readonly FunctionSymbol ctorDefinition;

    public ConstructorFunctionSymbol(FunctionSymbol ctorDefinition)
    {
        this.ctorDefinition = ctorDefinition;
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters() => this.InstantiatedType.GenericParameters
        .Select(p => new SynthetizedTypeParameterSymbol(this, p.Name))
        .Cast<TypeParameterSymbol>()
        .ToImmutableArray();

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.ConstructorSymbol.Parameters
        .Select(p => new SynthetizedParameterSymbol(this, p.Name, p.Type))
        .Cast<ParameterSymbol>()
        .ToImmutableArray();

    private TypeSymbol BuildReturnType() => this.Context is null
        ? this.InstantiatedType
        : this.InstantiatedType.GenericInstantiate(this.InstantiatedType.ContainingSymbol, this.Context.Value);

    private FunctionSymbol BuildConstructorSymbol() => this.Context is null
        ? this.ctorDefinition
        : this.ctorDefinition.GenericInstantiate(this.ReturnType, this.Context.Value);

    private GenericContext? BuildContext()
    {
        if (this.GenericParameters.Length == 0) return null;
        var substitutions = this.InstantiatedType.GenericParameters
            .Zip(this.GenericParameters)
            .ToImmutableDictionary(p => p.First, p => p.Second as TypeSymbol);
        return new(substitutions);
    }
}
