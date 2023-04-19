using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol : ParameterSymbol, ISourceSymbol
{
    public override TypeSymbol Type => this.type ??= this.BindType(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override ParameterSyntax DeclaringSyntax { get; }

    // TODO: Extracting parameter docs involves looking into the function docs and searching in the MD

    public SourceParameterSymbol(Symbol? containingSymbol, ParameterSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public void Bind(IBinderProvider binderProvider) =>
        this.BindType(binderProvider);

    private TypeSymbol BindType(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Type);
        return (TypeSymbol)binder.BindType(this.DeclaringSyntax.Type, binderProvider.DiagnosticBag);
    }
}
