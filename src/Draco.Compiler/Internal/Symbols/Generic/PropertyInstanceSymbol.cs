namespace Draco.Compiler.Internal.Symbols.Generic;

internal sealed class PropertyInstanceSymbol : PropertySymbol, IGenericInstanceSymbol
{
    public override TypeSymbol Type => this.type ??= this.BuildType();
    private TypeSymbol? type;

    public override FunctionSymbol? Getter => this.getter ??= this.BuildGetter();
    private FunctionSymbol? getter;

    public override FunctionSymbol? Setter => this.setter ??= this.BuildSetter();
    private FunctionSymbol? setter;

    public override string Name => this.GenericDefinition.Name;

    public override bool IsIndexer => this.GenericDefinition.IsIndexer;

    public override bool IsStatic => this.GenericDefinition.IsStatic;

    public override Symbol? ContainingSymbol { get; }
    public override PropertySymbol GenericDefinition { get; }

    public GenericContext Context { get; }

    public PropertyInstanceSymbol(Symbol? containingSymbol, PropertySymbol genericDefinition, GenericContext context)
    {
        this.ContainingSymbol = containingSymbol;
        this.GenericDefinition = genericDefinition;
        this.Context = context;
    }

    private TypeSymbol BuildType() =>
        this.GenericDefinition.Type.GenericInstantiate(this.GenericDefinition.Type.ContainingSymbol, this.Context);

    private FunctionSymbol? BuildGetter() =>
        this.GenericDefinition.Getter?.GenericInstantiate(this.ContainingSymbol, this.Context);

    private FunctionSymbol? BuildSetter() =>
        this.GenericDefinition.Setter?.GenericInstantiate(this.ContainingSymbol, this.Context);
}
