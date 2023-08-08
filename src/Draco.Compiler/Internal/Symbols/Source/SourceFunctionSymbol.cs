using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol, ISourceSymbol
{
    public override ImmutableArray<TypeParameterSymbol> GenericParameters => this.BindGenericParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<TypeParameterSymbol> genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters => this.BindParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<ParameterSymbol> parameters;

    public override TypeSymbol ReturnType => this.BindReturnTypeIfNeeded(this.DeclaringCompilation!);
    private TypeSymbol? returnType;

    public override Symbol ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;
    public override bool IsStatic => true;

    public override FunctionDeclarationSyntax DeclaringSyntax { get; }

    public BoundStatement Body => this.BindBodyIfNeeded(this.DeclaringCompilation!);
    private BoundStatement? body;

    public override string Documentation => this.DeclaringSyntax.Documentation;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        if (containingSymbol is null) throw new System.ArgumentNullException(nameof(containingSymbol));

        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindGenericParametersIfNeeded(binderProvider);
        this.BindParametersIfNeeded(binderProvider);
        // Force binding of parameters, as the type is lazy too
        foreach (var param in this.Parameters.Cast<SourceParameterSymbol>()) param.Bind(binderProvider);
        this.BindReturnTypeIfNeeded(binderProvider);
        var body = this.BindBodyIfNeeded(binderProvider);

        // Check, if this function collides with any other overloads that are visible from here
        this.CheckForSameParameterOverloads(binderProvider);

        // Flow analysis
        ReturnsOnAllPaths.Analyze(this, binderProvider.DiagnosticBag);
        DefiniteAssignment.Analyze(body, binderProvider.DiagnosticBag);
        ValAssignment.Analyze(this, binderProvider.DiagnosticBag);
    }

    private ImmutableArray<TypeParameterSymbol> BindGenericParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.genericParameters, () => this.BindGenericParameters(binderProvider));

    private ImmutableArray<TypeParameterSymbol> BindGenericParameters(IBinderProvider binderProvider)
    {
        // Simplest case if the function is not generic
        if (this.DeclaringSyntax.Generics is null) return ImmutableArray<TypeParameterSymbol>.Empty;

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

    private void CheckForSameParameterOverloads(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this);
        var discardBag = new DiagnosticBag();
        var overloads = binder.LookupValueSymbol(this.Name, this.DeclaringSyntax, discardBag);
        // If not found, do nothing
        if (overloads.IsError) return;
        // If this is the same instance, do nothing
        if (ReferenceEquals(overloads, this)) return;
        // Should not happen, but if not overload set, do nothing
        if (overloads is not OverloadSymbol overloadSymbol) return;
        // Check for same parameter types
        foreach (var func in overloadSymbol.Functions)
        {
            if (ReferenceEquals(func, this)) continue;
            if (!HasSameParameterTypes(func, this)) continue;

            // Report
            binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                template: TypeCheckingErrors.IllegalOverloadDefinition,
                location: this.DeclaringSyntax.Location,
                formatArgs: this.Name,
                relatedInformation: func.DeclaringSyntax is null
                    ? ImmutableArray<DiagnosticRelatedInformation>.Empty
                    : ImmutableArray.Create(DiagnosticRelatedInformation.Create(
                        location: func.DeclaringSyntax.Location,
                        format: "matching definition of {0}",
                        formatArgs: func.Name))));
        }
    }

    private ImmutableArray<ParameterSymbol> BindParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.parameters, () => this.BindParameters(binderProvider));

    private ImmutableArray<ParameterSymbol> BindParameters(IBinderProvider binderProvider)
    {
        var parameterSyntaxes = this.DeclaringSyntax.ParameterList.Values.ToList();
        var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();

        for (var i = 0; i < parameterSyntaxes.Count; ++i)
        {
            var parameterSyntax = parameterSyntaxes[i];
            var parameterName = parameterSyntax.Name.Text;

            var usedBefore = parameters.Any(p => p.Name == parameterName);
            if (usedBefore)
            {
                // NOTE: We only report later duplicates, no need to report the first instance
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: parameterSyntax.Location,
                    formatArgs: parameterName));
            }

            if (parameterSyntax.Variadic is not null && i != parameterSyntaxes.Count - 1)
            {
                binderProvider.DiagnosticBag.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.VariadicParameterNotLast,
                    location: parameterSyntax.Location,
                    formatArgs: parameterName));
            }

            var parameter = new SourceParameterSymbol(this, parameterSyntax);
            parameters.Add(parameter);
        }

        return parameters.ToImmutable();
    }

    private TypeSymbol BindReturnTypeIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeNull(ref this.returnType, () => this.BindReturnType(binderProvider));

    private TypeSymbol BindReturnType(IBinderProvider binderProvider)
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclaringSyntax.ReturnType is null) return IntrinsicSymbols.Unit;

        // Otherwise, we need to resolve
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.ReturnType.Type, binderProvider.DiagnosticBag);
    }

    private BoundStatement BindBodyIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeNull(ref this.body, () => this.BindBody(binderProvider));

    private BoundStatement BindBody(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Body);
        return binder.BindFunction(this, binderProvider.DiagnosticBag);
    }

    private static bool HasSameParameterTypes(FunctionSymbol f1, FunctionSymbol f2)
    {
        if (f1.Parameters.Length != f2.Parameters.Length) return false;

        for (var i = 0; i < f1.Parameters.Length; ++i)
        {
            var p1 = f1.Parameters[i];
            var p2 = f2.Parameters[i];

            if (!SymbolEqualityComparer.SignatureMatch.Equals(p1.Type, p2.Type)) return false;
        }

        return true;
    }
}
