using Draco.Compiler.Api.Semantics;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a parameter in a function.
/// </summary>
internal abstract partial class ParameterSymbol : LocalSymbol
{
    public override bool IsMutable => false;

    public override Visibility Visibility => this.ContainingSymbol is null ? Visibility.Internal : this.ContainingSymbol.Visibility;

    public override IParameterSymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);

    public override void Accept(SymbolVisitor visitor) => visitor.VisitParameter(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitParameter(this);

    public override string ToString() => string.IsNullOrWhiteSpace(this.Name)
        ? this.Type.ToString()
        : $"{this.Name}: {this.Type}";
}
