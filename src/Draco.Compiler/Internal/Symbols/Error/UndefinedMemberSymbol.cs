using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined member reference.
/// </summary>
internal sealed class UndefinedMemberSymbol : Symbol, ITypedSymbol, IMemberSymbol
{
    public override bool IsError => true;

    public override Symbol? ContainingSymbol => null;

    public TypeSymbol Type => IntrinsicSymbols.ErrorType;

    public bool IsStatic => true;

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
