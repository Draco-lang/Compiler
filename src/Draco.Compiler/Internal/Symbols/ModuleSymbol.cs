namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a compilation unit.
/// </summary>
internal abstract partial class ModuleSymbol : Symbol
{
    public override void Accept(SymbolVisitor visitor) => visitor.VisitModule(this);
    public override TResult Accept<TResult>(SymbolVisitor<TResult> visitor) => visitor.VisitModule(this);
}
