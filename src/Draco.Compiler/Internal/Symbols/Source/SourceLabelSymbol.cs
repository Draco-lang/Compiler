using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source label definition.
/// </summary>
internal sealed class SourceLabelSymbol(
    Symbol containingSymbol,
    LabelDeclarationSyntax declarationSyntax) : LabelSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override LabelDeclarationSyntax DeclaringSyntax { get; } = declarationSyntax;

    public void Bind(IBinderProvider binderProvider) { }
}
