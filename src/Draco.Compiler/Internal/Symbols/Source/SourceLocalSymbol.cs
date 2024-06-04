using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source local declaration.
/// </summary>
internal sealed class SourceLocalSymbol : LocalSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; }
    public override string Name { get; }
    public override bool IsMutable { get; }
    public override SyntaxNode DeclaringSyntax { get; }

    public override TypeSymbol Type => this.type.Substitution;
    private readonly TypeSymbol type;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    public SourceLocalSymbol(Symbol containingSymbol, string name, TypeSymbol type, bool isMutable, SyntaxNode declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.Name = name;
        this.type = type;
        this.IsMutable = isMutable;
        this.DeclaringSyntax = declaringSyntax;
    }

    public SourceLocalSymbol(Symbol containingSymbol, TypeSymbol type, VariableDeclarationSyntax syntax)
        : this(containingSymbol, syntax.Name.Text, type, syntax.Keyword.Kind == TokenKind.KeywordVar, syntax)
    {
    }

    public void Bind(IBinderProvider binderProvider) { }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
