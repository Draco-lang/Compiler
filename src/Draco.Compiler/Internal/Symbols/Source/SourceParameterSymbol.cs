using System.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override Type Type => this.type ??= this.BuildType();
    private Type? type;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public ParameterSyntax DeclarationSyntax { get; }
    SyntaxNode ISourceSymbol.DeclarationSyntax => this.DeclarationSyntax;

    // TODO: Extracting parameter docs involves looking into the function docs and searching in the MD

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public override ISymbol ToApiSymbol() => new Api.Semantics.ParameterSymbol(this);

    private Type BuildType()
    {
        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Type);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        var typeSymbol = (TypeSymbol)binder.BindType(this.DeclarationSyntax.Type, diagnostics);

        return typeSymbol.Type;
    }
}
