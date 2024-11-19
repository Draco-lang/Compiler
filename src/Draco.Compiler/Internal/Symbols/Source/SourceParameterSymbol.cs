using System.Collections.Immutable;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// A function parameter defined in-source.
/// </summary>
internal sealed class SourceParameterSymbol(
    FunctionSymbol containingSymbol,
    NormalParameterSyntax syntax) : ParameterSymbol, ISourceSymbol
{
    public override ImmutableArray<AttributeInstance> Attributes => this.BindAttributesIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<AttributeInstance> attributes;

    public override TypeSymbol Type => this.BindTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? type;

    public override FunctionSymbol ContainingSymbol { get; } = containingSymbol;
    public override bool IsVariadic => this.DeclaringSyntax.Variadic is not null;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override NormalParameterSyntax DeclaringSyntax { get; } = syntax;

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindTypeIfNeeded(binderProvider);
        this.BindAttributesIfNeeded(binderProvider);
    }

    private ImmutableArray<AttributeInstance> BindAttributesIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.attributes, () => this.BindAttributes(binderProvider));

    private ImmutableArray<AttributeInstance> BindAttributes(IBinderProvider binderProvider)
    {
        var attrsSyntax = this.DeclaringSyntax.Attributes;
        if (attrsSyntax is null) return [];

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindAttributeList(this, attrsSyntax, binderProvider.DiagnosticBag);
    }

    private TypeSymbol BindTypeIfNeeded(IBinderProvider binderProvider) =>
        LazyInitializer.EnsureInitialized(ref this.type, () => this.BindType(binderProvider));

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
