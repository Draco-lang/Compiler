using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A synthetized constructor function for metadata types.
/// </summary>
internal sealed class SynthetizedMetadataConstructorSymbol : SynthetizedFunctionSymbol
{
    public override string Name => this.instantiatedType.Name;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, this.BuildGenericParameters);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters =>
        InterlockedUtils.InitializeDefault(ref this.parameters, this.BuildParameters);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => InterlockedUtils.InitializeNull(ref this.returnType, this.BuildReturnType);
    private TypeSymbol? returnType;

    public override BoundStatement Body => InterlockedUtils.InitializeNull(ref this.body, this.BuildBody);
    private BoundStatement? body;

    private FunctionSymbol ConstructorSymbol => InterlockedUtils.InitializeNull(ref this.constructorSymbol, this.BuildConstructorSymbol);
    private FunctionSymbol? constructorSymbol;

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

    public SynthetizedMetadataConstructorSymbol(MetadataTypeSymbol instantiatedType, MethodDefinition ctorDefinition)
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

    private BoundStatement BuildBody() => ExpressionStatement(ReturnExpression(
        value: ObjectCreationExpression(
            objectType: this.ReturnType,
            constructor: this.ConstructorSymbol,
            arguments: this.Parameters
                .Select(ParameterExpression)
                .Cast<BoundExpression>()
                .ToImmutableArray())));

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
