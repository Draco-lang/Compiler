using System;
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
    public override ImmutableArray<TypeParameterSymbol> GenericParameters =>
        this.genericParameters ??= this.BindGenericParameters(this.DeclaringCompilation!.GlobalDiagnosticBag);
    private ImmutableArray<TypeParameterSymbol>? genericParameters;

    public override ImmutableArray<ParameterSymbol> Parameters =>
        this.parameters ??= this.BindParameters(this.DeclaringCompilation!.GlobalDiagnosticBag);
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType =>
        this.returnType ??= this.BindReturnType(this.DeclaringCompilation!);
    private TypeSymbol? returnType;

    public override Symbol ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override FunctionDeclarationSyntax DeclaringSyntax { get; }

    public BoundStatement Body => this.body ??= this.BindBody(this.DeclaringCompilation!);
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
        this.BindGenericParameters(binderProvider.DiagnosticBag);
        this.BindParameters(binderProvider.DiagnosticBag);
        this.BindReturnType(binderProvider);
        this.BindBody(binderProvider);

        // Flow analysis
        ReturnsOnAllPaths.Analyze(this, binderProvider.DiagnosticBag);
        DefiniteAssignment.Analyze(this.Body, binderProvider.DiagnosticBag);
        ValAssignment.Analyze(this, binderProvider.DiagnosticBag);
    }

    private ImmutableArray<TypeParameterSymbol> BindGenericParameters(DiagnosticBag diagnostics)
    {
        // TODO
        throw new NotImplementedException();
    }

    private ImmutableArray<ParameterSymbol> BindParameters(DiagnosticBag diagnostics)
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
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.IllegalShadowing,
                    location: parameterSyntax.Location,
                    formatArgs: parameterName));
            }

            var parameter = new SourceParameterSymbol(this, parameterSyntax);
            parameters.Add(parameter);
        }

        return parameters.ToImmutable();
    }

    private TypeSymbol BindReturnType(IBinderProvider binderProvider)
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclaringSyntax.ReturnType is null) return IntrinsicSymbols.Unit;

        // Otherwise, we need to resolve
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return binder.BindTypeToTypeSymbol(this.DeclaringSyntax.ReturnType.Type, binderProvider.DiagnosticBag);
    }

    private BoundStatement BindBody(IBinderProvider binderProvider)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Body);
        return binder.BindFunction(this, binderProvider.DiagnosticBag);
    }
}
