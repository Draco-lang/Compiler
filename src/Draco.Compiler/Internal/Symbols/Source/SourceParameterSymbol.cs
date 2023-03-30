using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol
{
    public override Type Type => this.type ??= this.BuildType();
    private Type? type;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public override ParameterSyntax DeclarationSyntax { get; }

    // TODO: Extracting parameter docs involves looking into the function docs and searching in the MD

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    private Type BuildType()
    {
        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Type);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        var typeSymbol = (TypeSymbol)binder.BindType(this.DeclarationSyntax.Type, diagnostics);

        return typeSymbol.Type;
    }
}
