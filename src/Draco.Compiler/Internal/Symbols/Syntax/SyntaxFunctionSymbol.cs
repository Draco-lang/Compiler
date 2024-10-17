using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Symbols.Syntax;

/// <summary>
/// The base for function symbols defined based on some syntax.
/// </summary>
internal abstract class SyntaxFunctionSymbol(
    Symbol containingSymbol,
    FunctionDeclarationSyntax syntax) : FunctionSymbol, ISourceSymbol
{
    public override Symbol ContainingSymbol => containingSymbol;
    public override FunctionDeclarationSyntax DeclaringSyntax => syntax;
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override Api.Semantics.Visibility Visibility =>
        GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override ImmutableArray<AttributeInstance> Attributes => this.BindAttributesIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<AttributeInstance> attributes;

    public override ImmutableArray<TypeParameterSymbol> GenericParameters => this.BindGenericParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters => this.BindParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<ParameterSymbol> parameters;

    public SourceThisParameterSymbol? ThisArgument
    {
        get
        {
            this.BindParametersIfNeeded(this.DeclaringCompilation!);
            return this.thisArgument;
        }
    }
    private SourceThisParameterSymbol? thisArgument;

    public override TypeSymbol ReturnType => this.BindReturnTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? returnType;

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => this.DeclaringSyntax.Documentation;

    public override abstract BoundStatement Body { get; }

    public abstract void Bind(IBinderProvider binderProvider);

    protected ImmutableArray<AttributeInstance> BindAttributesIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.attributes, () => this.BindAttributes(binderProvider));

    private ImmutableArray<AttributeInstance> BindAttributes(IBinderProvider binderProvider)
    {
        var attrsSyntax = this.DeclaringSyntax.Attributes;
        if (attrsSyntax is null) return [];

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindAttributeList(this, attrsSyntax, binderProvider.DiagnosticBag);
    }

    protected ImmutableArray<TypeParameterSymbol> BindGenericParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, () => this.BindGenericParameters(binderProvider));

    private ImmutableArray<TypeParameterSymbol> BindGenericParameters(IBinderProvider binderProvider)
    {
        // Simplest case if the function is not generic
        if (this.DeclaringSyntax.Generics is null) return [];

        var genericParamSyntaxes = this.DeclaringSyntax.Generics.Parameters.Values.ToList();
        var genericParams = ImmutableArray.CreateBuilder<TypeParameterSymbol>();

        foreach (var genericParamSyntax in genericParamSyntaxes)
        {
            var paramName = genericParamSyntax.Name.Text;

            var usedBefore = genericParams.Any(p => p.Name == paramName);
            if (usedBefore)
            {
                // NOTE: We only report later duplicates, no need to report the first instance
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: genericParamSyntax.Location,
                    formatArgs: paramName));
            }

            var genericParam = new SourceTypeParameterSymbol(this, genericParamSyntax);
            genericParams.Add(genericParam);
        }

        return genericParams.ToImmutable();
    }

    protected ImmutableArray<ParameterSymbol> BindParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.parameters, () => this.BindParameters(binderProvider));

    private ImmutableArray<ParameterSymbol> BindParameters(IBinderProvider binderProvider)
    {
        var parameterSyntaxes = this.DeclaringSyntax.ParameterList.Values.ToList();
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

        var isInstance = null as SourceThisParameterSymbol;

        for (var i = 0; i < parameterSyntaxes.Count; ++i)
        {
            var syntax = parameterSyntaxes[i];

            if (syntax is ThisParameterSyntax thisParameter)
            {
                var asSymbol = new SourceThisParameterSymbol(this, thisParameter);
                if (i == 0)
                {
                    this.thisArgument = asSymbol;
                    isInstance = asSymbol;
                    continue;
                }
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.ThisParameterNotFirst,
                    location: thisParameter.Location));
                parameters.Add(asSymbol);
                continue;
            }
            var parameterSyntax = (ParameterSyntax)syntax;

            var parameterName = parameterSyntax.Name.Text;

            var usedBefore = parameters.Any(p => p.Name == parameterName);
            if (usedBefore)
            {
                // NOTE: We only report later duplicates, no need to report the first instance
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: syntax.Location,
                    formatArgs: parameterName));
            }

            if (parameterSyntax.Variadic is not null && i != parameterSyntaxes.Count - 1)
            {
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.VariadicParameterNotLast,
                    location: syntax.Location,
                    formatArgs: parameterName));
            }

            var parameter = new SourceParameterSymbol(this, parameterSyntax);
            parameters.Add(parameter);
        }

        return parameters.ToImmutable();
    }

    protected TypeSymbol BindReturnTypeIfNeeded(IBinderProvider binderProvider) =>
        LazyInitializer.EnsureInitialized(ref this.returnType, () => this.BindReturnType(binderProvider));

    private TypeSymbol BindReturnType(IBinderProvider binderProvider)
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclaringSyntax.ReturnType is null) return WellKnownTypes.Unit;

        // Otherwise, we need to resolve
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.ReturnType.Type, binderProvider.DiagnosticBag);
    }

    private SymbolDocumentation BuildDocumentation() =>
        MarkdownDocumentationExtractor.Extract(this);
}
