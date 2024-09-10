namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// An error property symbol.
/// </summary>
internal class ErrorPropertySymbol : PropertySymbol
{
    public static FunctionSymbol CreateIndexerGet(int indicesCount) =>
        new ErrorIndexerPropertySymbol(indicesCount).Getter;

    public static FunctionSymbol CreateIndexerSet(int indicesCount) =>
        new ErrorIndexerPropertySymbol(indicesCount).Setter;

    public override bool IsError => true;
    public override bool IsStatic => true;
    public override bool IsExplicitImplementation => false;
    public override bool IsIndexer => false;
    public override TypeSymbol Type => WellKnownTypes.ErrorType;

    protected virtual int ParameterCount => 0;

    public override FunctionSymbol Getter =>
        this.getter ??= new ErrorPropertyAccessorSymbol(this, parameterCount: this.ParameterCount);
    private FunctionSymbol? getter;

    public override FunctionSymbol Setter =>
        this.setter ??= new ErrorPropertyAccessorSymbol(this, parameterCount: this.ParameterCount + 1);
    private FunctionSymbol? setter;
}
