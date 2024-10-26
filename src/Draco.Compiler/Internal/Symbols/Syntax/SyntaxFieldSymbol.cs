using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Syntax;

/// <summary>
/// The base for field symbols defined based on some syntax.
/// </summary>
internal abstract class SyntaxFieldSymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : FieldSymbol, ISourceSymbol
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

    public abstract void Bind(IBinderProvider binderProvider);

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
