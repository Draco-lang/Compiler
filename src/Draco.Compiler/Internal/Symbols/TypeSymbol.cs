namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    /// <summary>
    /// True, if this type is a value-type.
    /// </summary>
    public virtual bool IsValueType => false;

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.TypeSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitType(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitType(this);

    public override abstract string ToString();
}
