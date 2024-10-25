using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Documentation;
using System.Threading;
using System;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Syntax;

/// <summary>
/// The base for auto-properties defined based on some syntax.
/// </summary>
internal abstract class SyntaxAutoPropertySymbol : PropertySymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol { get; }
    public override VariableDeclarationSyntax DeclaringSyntax { get; }

    public override string Name => this.DeclaringSyntax.Name.Text;
    public override bool IsIndexer => false;
    public override bool IsExplicitImplementation => false;
    // NOTE: In the future we probably want to check the global modifier if it's in a class
    public override bool IsStatic => this.ContainingSymbol is not TypeSymbol;

    public override Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    protected SyntaxAutoPropertySymbol(Symbol containingSymbol, VariableDeclarationSyntax syntax)
    {
        if (syntax.FieldModifier is not null) throw new ArgumentException("a property must not have the field modifier", nameof(syntax));

        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public abstract void Bind(IBinderProvider binderProvider);

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
