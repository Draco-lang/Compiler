namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an error property for some failure in the binding process.
/// </summary>
internal sealed class ErrorPropertySymbol : PropertySymbol
{
    public override FunctionSymbol Getter { get; }
    public override FunctionSymbol Setter { get; }
    public override string Name { get; }

    public override TypeSymbol Type => WellKnownTypes.ErrorType;

    public override bool IsError => true;
    public override bool IsIndexer => false;
    public override bool IsStatic => false;

    public ErrorPropertySymbol(string name)
    {
        this.Name = name;
        this.Getter = new UndefinedPropertyAccessorSymbol(this);
        this.Setter = new UndefinedPropertyAccessorSymbol(this);
    }
}
