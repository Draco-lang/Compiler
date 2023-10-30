using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override Symbol ContainingSymbol { get; }
    public override bool IsVariadic => this.DeclaringSyntax.Variadic is not null;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override ParameterSyntax DeclaringSyntax { get; }

    // TODO: Extracting parameter docs involves looking into the function docs and searching in the MD

    public SourceParameterSymbol(Symbol containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public void Bind(IBinderProvider binderProvider) =>
        this.BindTypeIfNeeded(binderProvider);

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeNull(ref this.type, () => this.BindType(binderProvider));

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Type);
        var diagnostics = binderProvider.DiagnosticBag;

        var result = binder.BindTypeToTypeSymbol(this.DeclaringSyntax.Type, diagnostics);

        // Check if this is a legal variadic type
        if (this.IsVariadic && !result.IsError && !BinderFacts.TryGetVariadicElementType(result, out _))
        {
            // It's not
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.IllegalVariadicType,
                location: this.DeclaringSyntax.Type.Location,
                formatArgs: result));
        }

        return result;
    }
}
