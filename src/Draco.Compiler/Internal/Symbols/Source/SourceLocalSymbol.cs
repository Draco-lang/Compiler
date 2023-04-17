using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source local declaration.
/// </summary>
internal sealed class SourceLocalSymbol : LocalSymbol, ISourceSymbol
{
    public override TypeSymbol Type { get; }

    public override Symbol? ContainingSymbol => this.untypedSymbol.ContainingSymbol;
    public override string Name => this.untypedSymbol.Name;

    public override VariableDeclarationSyntax DeclaringSyntax => this.untypedSymbol.DeclaringSyntax;

    public override bool IsMutable => this.untypedSymbol.IsMutable;

    public override string Documentation => this.DeclaringSyntax.Documentation;

    private readonly UntypedLocalSymbol untypedSymbol;

    public SourceLocalSymbol(UntypedLocalSymbol untypedSymbol, TypeSymbol type)
    {
        this.untypedSymbol = untypedSymbol;
        this.Type = type;
    }

    public void Bind(DiagnosticBag diagnostics) { }
}
