using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A constructor function for types.
/// </summary>
internal sealed class ConstructorFunctionSymbol : IrFunctionSymbol
{
    public override string Name => this.InstantiatedType.Name;
    public override bool IsSpecialName => true;
    public override bool IsStatic => true;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => InterlockedUtils.InitializeNull(ref this.returnType, this.BuildReturnType);
    private TypeSymbol? returnType;

    public override CodegenDelegate Codegen => (codegen, target, args) =>
    {
        var instance = target.Type;
        var ctorSymbol = this.constructorSymbol;
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

    private TypeSymbol InstantiatedType => (TypeSymbol)this.constructorSymbol.ContainingSymbol!;

    private readonly FunctionSymbol constructorSymbol;

    public ConstructorFunctionSymbol(FunctionSymbol ctorSymbol)
    {
        this.constructorSymbol = ctorSymbol;
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters() => this.InstantiatedType.GenericParameters
        .Select(p => new SynthetizedTypeParameterSymbol(this, p.Name))
        .Cast<TypeParameterSymbol>()
        .ToImmutableArray();

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.constructorSymbol.Parameters
        .Select(p => new SynthetizedParameterSymbol(this, p.Name, p.Type))
        .Cast<ParameterSymbol>()
        .ToImmutableArray();

    private TypeSymbol BuildReturnType() => this.Context is null
        ? this.InstantiatedType
        : this.InstantiatedType.GenericInstantiate(this.InstantiatedType.ContainingSymbol, this.Context.Value);

    private GenericContext? BuildContext()
    {
        if (this.GenericParameters.Length == 0) return null;
        var substitutions = this.InstantiatedType.GenericParameters
            .Zip(this.GenericParameters)
            .ToImmutableDictionary(p => p.First, p => p.Second as TypeSymbol);
        return new(substitutions);
    }
}
