using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source label definition.
/// </summary>
internal sealed class SourceLabelSymbol : LabelSymbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public override LabelDeclarationSyntax DeclarationSyntax { get; }

    public SourceLabelSymbol(Symbol? containingSymbol, LabelDeclarationSyntax declarationSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = declarationSyntax;
    }

    public void Bind(DiagnosticBag diagnostics) { }
}
