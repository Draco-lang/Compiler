using System.Threading;

namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated parameter.
/// The parameter definition itself is not generic, but the parameter was within a generic context.
/// </summary>
internal sealed class ParameterInstanceSymbol(
    FunctionSymbol containingSymbol,
    ParameterSymbol genericDefinition,
    GenericContext context) : ParameterSymbol, IGenericInstanceSymbol
{
    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override bool IsVariadic => this.GenericDefinition.IsVariadic;
    public override string Name => this.GenericDefinition.Name;

    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;
    public override ParameterSymbol GenericDefinition { get; } = genericDefinition;

    public GenericContext Context { get; } = context;

    private TypeSymbol BuildType() =>
        this.GenericDefinition.Type.GenericInstantiate(this.GenericDefinition.Type.ContainingSymbol, this.Context);
}
