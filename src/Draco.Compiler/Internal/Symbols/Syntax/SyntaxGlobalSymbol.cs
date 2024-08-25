using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Syntax;

/// <summary>
/// The base for global symbols defined based on some syntax.
/// </summary>
internal abstract class SyntaxGlobalSymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : GlobalSymbol
{
    public override Symbol ContainingSymbol => containingSymbol;
    public override VariableDeclarationSyntax DeclaringSyntax => syntax;

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
