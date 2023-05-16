using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A generic type parameter defined in-source.
/// </summary>
internal class SourceTypeParameterSymbol : TypeParameterSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override GenericParameterSyntax DeclaringSyntax { get; }

    public SourceTypeParameterSymbol(Symbol containingSymbol, GenericParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public void Bind(IBinderProvider binderProvider) { }
}
