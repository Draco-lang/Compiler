using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A generic class defined in-source.
/// </summary>
internal sealed class SourceClassSymbol(
    Symbol containingSymbol,
    ClassDeclarationSyntax syntax) : TypeSymbol, ISourceSymbol
{
    public override Symbol? ContainingSymbol { get; } = containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override ClassDeclarationSyntax DeclaringSyntax => syntax;

    public void Bind(IBinderProvider binderProvider)
    {
        this.Bind
    }


    private protected string GenericsToString()
    {
        if (this.IsGenericDefinition) return $"<{string.Join(", ", this.GenericParameters)}>";
        return string.Empty;
    }

    public override string ToString() => $"{this.Name}<{}>;
}
