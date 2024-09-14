namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a member reference with errors.
/// </summary>
internal sealed class ErrorMemberSymbol : Symbol, ITypedSymbol, IMemberSymbol
{
    /// <summary>
    /// A singleton instance to use.
    /// </summary>
    public static ErrorMemberSymbol Instance { get; } = new();

    public override bool IsError => true;
    public override Api.Semantics.Visibility Visibility => Api.Semantics.Visibility.Public;
    public override Api.Semantics.SymbolKind Kind => Api.Semantics.SymbolKind.Field;

    public TypeSymbol Type => WellKnownTypes.ErrorType;

    public bool IsStatic => true;
    public bool IsExplicitImplementation => false;

    private ErrorMemberSymbol()
    {
    }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.AnySymbol(this);

    public override void Accept(SymbolVisitor visitor) => throw new System.NotSupportedException();
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => throw new System.NotSupportedException();
}
