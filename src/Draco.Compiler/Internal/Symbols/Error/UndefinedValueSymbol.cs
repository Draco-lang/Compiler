namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined, in-source value reference.
/// </summary>
internal sealed class UndefinedValueSymbol(string name) : Symbol, ITypedSymbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; } = name;

    public TypeSymbol Type => WellKnownTypes.ErrorType;

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
