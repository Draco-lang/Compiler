namespace Draco.Compiler.Internal.Symbols.Generic;

/// <summary>
/// Represents a generic instantiated static field.
/// The field definition itself is not generic, but the field was within a generic context.
/// </summary>
internal sealed class GlobalInstanceSymbol : GlobalSymbol, IGenericInstanceSymbol
{
    public override TypeSymbol Type => InterlockedUtils.InitializeNull(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override string Name => this.GenericDefinition.Name;

    public override bool IsMutable => this.GenericDefinition.IsMutable;

    public override Symbol? ContainingSymbol { get; }
    public override GlobalSymbol GenericDefinition { get; }

    public GenericContext Context { get; }

    public GlobalInstanceSymbol(Symbol? containingSymbol, GlobalSymbol genericDefinition, GenericContext context)
    {
        this.ContainingSymbol = containingSymbol;
        this.GenericDefinition = genericDefinition;
        this.Context = context;
    }

    private TypeSymbol BuildType() =>
        this.GenericDefinition.Type.GenericInstantiate(this.GenericDefinition.Type.ContainingSymbol, this.Context);
}
