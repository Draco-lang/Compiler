using System.Diagnostics;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol
{
    public override TypeSymbol Type => this.type ??= this.BuildType();
    private TypeSymbol? type;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public override ParameterSyntax DeclarationSyntax { get; }

    // TODO: Extracting parameter docs involves looking into the function docs and searching in the MD

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    private TypeSymbol BuildType()
    {
        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Type);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        return (TypeSymbol)binder.BindType(this.DeclarationSyntax.Type, diagnostics);
    }
}
