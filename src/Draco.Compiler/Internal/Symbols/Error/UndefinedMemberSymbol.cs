namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents an undefined member reference.
/// </summary>
internal sealed class UndefinedMemberSymbol : Symbol, ITypedSymbol, IMemberSymbol
{
    /// <summary>
    /// A singleton instance to use.
    /// </summary>
    public static UndefinedMemberSymbol Instance { get; } = new();

    public override bool IsError => true;

    public TypeSymbol Type => WellKnownTypes.ErrorType;

    public bool IsStatic => true;

    private UndefinedMemberSymbol()
    {
    }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
