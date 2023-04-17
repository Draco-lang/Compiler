using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source label definition.
/// </summary>
internal sealed class SourceLabelSymbol : LabelSymbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override LabelDeclarationSyntax DeclaringSyntax { get; }

    public SourceLabelSymbol(Symbol? containingSymbol, LabelDeclarationSyntax declarationSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = declarationSyntax;
    }

    public void Bind(DiagnosticBag diagnostics) { }
}
