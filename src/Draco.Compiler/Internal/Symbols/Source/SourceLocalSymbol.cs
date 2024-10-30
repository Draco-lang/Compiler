using System;
using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source local declaration.
/// </summary>
internal sealed class SourceLocalSymbol(
    Symbol containingSymbol,
    string name,
    TypeSymbol type,
    bool isMutable,
    SyntaxNode declaringSyntax) : LocalSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; } = containingSymbol;
    public override string Name { get; } = name;
    public override bool IsMutable { get; } = isMutable;
    public override SyntaxNode DeclaringSyntax { get; } = declaringSyntax;

    public override TypeSymbol Type => type.Substitution;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    public SourceLocalSymbol(Symbol containingSymbol, TypeSymbol type, VariableDeclarationSyntax syntax)
        : this(containingSymbol, syntax.Name.Text, type, syntax.Keyword.Kind == TokenKind.KeywordVar, syntax)
    {
        if (syntax.FieldModifier is not null) throw new ArgumentException("local symbols cannot have field modifiers");
    }

    public void Bind(IBinderProvider binderProvider) { }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
