using System.Threading;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.FlowAnalysis;

namespace Draco.Compiler.Internal.Symbols.Source;

internal sealed class SourceGlobalSymbol(
    Symbol containingSymbol,
    VariableDeclarationSyntax syntax) : GlobalSymbol, ISourceSymbol
{
    public override TypeSymbol Type => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Type;

    public override bool IsMutable => this.DeclaringSyntax.Keyword.Kind == TokenKind.KeywordVar;
    public override Symbol ContainingSymbol => containingSymbol;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override VariableDeclarationSyntax DeclaringSyntax => syntax;

    public BoundExpression? Value => this.BindTypeAndValueIfNeeded(this.DeclaringCompilation!).Value;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    // IMPORTANT: flag is type, needs to be written last
    // NOTE: We check the TYPE here, as value is nullable
    private bool NeedsBuild => Volatile.Read(ref this.type) is null;

    private TypeSymbol? type;
    private BoundExpression? value;

    private readonly object buildLock = new();

    public SourceGlobalSymbol(Symbol containingSymbol, GlobalDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public void Bind(IBinderProvider binderProvider)
    {
        var (_, value) = this.BindTypeAndValueIfNeeded(binderProvider);

        // Flow analysis
        if (value is not null) DefiniteAssignment.Analyze(value, binderProvider.DiagnosticBag);
        ValAssignment.Analyze(this, binderProvider.DiagnosticBag);
    }

    private (TypeSymbol Type, BoundExpression? Value) BindTypeAndValueIfNeeded(IBinderProvider binderProvider)
    {
        if (!this.NeedsBuild) return (this.type!, this.value);
        lock (this.buildLock)
        {
            // NOTE: We check the TYPE here, as value is nullable,
            // but a type always needs to be inferred
            if (this.NeedsBuild)
            {
                var (type, value) = this.BindTypeAndValue(binderProvider);
                this.value = value;
                // IMPORTANT: type is flag, written last
                Volatile.Write(ref this.type, type);
            }
            return (this.type!, this.value);
        }
    }

    private GlobalBinding BindTypeAndValue(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindGlobal(this, binderProvider.DiagnosticBag);
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
