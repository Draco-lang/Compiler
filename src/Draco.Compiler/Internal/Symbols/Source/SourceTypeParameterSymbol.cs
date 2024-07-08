using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A generic type parameter defined in-source.
/// </summary>
internal sealed class SourceTypeParameterSymbol(
    Symbol containingSymbol,
    GenericParameterSyntax syntax) : TypeParameterSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override GenericParameterSyntax DeclaringSyntax { get; } = syntax;

    public void Bind(IBinderProvider binderProvider) { }
}
