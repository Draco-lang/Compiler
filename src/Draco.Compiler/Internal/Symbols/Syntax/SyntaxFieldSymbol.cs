using System;
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
internal abstract class SyntaxFieldSymbol : FieldSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; }
    public override VariableDeclarationSyntax DeclaringSyntax { get; }

    // NOTE: In the future we probably want to check the global modifier if it's in a class
    public override bool IsStatic => this.ContainingSymbol is not TypeSymbol;
    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    public SyntaxFieldSymbol(Symbol containingSymbol, VariableDeclarationSyntax syntax)
    {
        if (syntax.FieldModifier is null) throw new ArgumentException("fields must have field modifiers");

        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    public abstract void Bind(IBinderProvider binderProvider);

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
