namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a faulty in-source value reference - like a local variable.
/// </summary>
internal sealed class ErrorValueSymbol(string name) : Symbol, ITypedSymbol
{
    public override bool IsError => true;
    public override Symbol? ContainingSymbol => null;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;

    public override string Name { get; } = name;

    public TypeSymbol Type => WellKnownTypes.ErrorType;

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
