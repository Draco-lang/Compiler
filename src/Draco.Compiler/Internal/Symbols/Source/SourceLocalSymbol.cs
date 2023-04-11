using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source local declaration.
/// </summary>
internal sealed class SourceLocalSymbol : LocalSymbol
{
    public override TypeSymbol Type { get; }

    public override Symbol? ContainingSymbol => this.untypedSymbol.ContainingSymbol;
    public override string Name => this.untypedSymbol.Name;

    public override VariableDeclarationSyntax DeclarationSyntax => this.untypedSymbol.DeclarationSyntax;

    public override bool IsMutable => this.untypedSymbol.IsMutable;

    public override string Documentation => this.DeclarationSyntax.Documentation;

    private readonly UntypedLocalSymbol untypedSymbol;

    public SourceLocalSymbol(UntypedLocalSymbol untypedSymbol, TypeSymbol type)
    {
        this.untypedSymbol = untypedSymbol;
        this.Type = type;
    }
}
