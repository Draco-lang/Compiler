using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A constructor function for a metadata type.
/// </summary>
internal sealed class MetadataConstructorFunctionSymbol : IrFunctionSymbol
{
    public override string Name => this.instantiatedType.Name;
    public override Symbol? ContainingSymbol => null;
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

    private readonly MetadataTypeSymbol instantiatedType;
    private readonly MethodDefinition ctorDefinition;

    public MetadataConstructorFunctionSymbol(MetadataTypeSymbol instantiatedType, MethodDefinition ctorDefinition)
    {
        this.instantiatedType = instantiatedType;
        this.ctorDefinition = ctorDefinition;
    }

    private ImmutableArray<TypeParameterSymbol> BuildGenericParameters() => this.instantiatedType.GenericParameters
        .Select(p => new SynthetizedTypeParameterSymbol(this, p.Name))
        .Cast<TypeParameterSymbol>()
        .ToImmutableArray();

    private ImmutableArray<ParameterSymbol> BuildParameters() => this.ConstructorSymbol.Parameters
        .Select(p => new SynthetizedParameterSymbol(this, p.Name, p.Type))
        .Cast<ParameterSymbol>()
        .ToImmutableArray();

    private TypeSymbol BuildReturnType() => this.Context is null
        ? this.instantiatedType
        : this.instantiatedType.GenericInstantiate(this.instantiatedType.ContainingSymbol, this.Context.Value);

    private FunctionSymbol BuildConstructorSymbol()
    {
        var ctorSymbol = new MetadataMethodSymbol(this.ReturnType, this.ctorDefinition) as FunctionSymbol;
        if (this.Context is not null) ctorSymbol = ctorSymbol.GenericInstantiate(this.ReturnType, this.Context.Value);
        return ctorSymbol;
    }

    private GenericContext? BuildContext()
    {
        if (this.GenericParameters.Length == 0) return null;
        var substitutions = this.instantiatedType.GenericParameters
            .Zip(this.GenericParameters)
            .ToImmutableDictionary(p => p.First, p => p.Second as TypeSymbol);
        return new(substitutions);
    }
}
