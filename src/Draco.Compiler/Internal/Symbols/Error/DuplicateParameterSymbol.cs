using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Error;

/// <summary>
/// Represents a duplicate parameter.
/// </summary>
internal sealed class DuplicateParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override Types.Type Type => Types.Intrinsics.Error;
    public override bool IsError => true;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public ParameterSyntax DeclarationSyntax { get; }
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    public DuplicateParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public override Api.Semantics.ISymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);
}
