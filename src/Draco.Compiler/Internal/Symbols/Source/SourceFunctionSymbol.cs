using System.Collections.Immutable;
using System.Diagnostics;
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
    public override ImmutableArray<ParameterSymbol> Parameters =>
        this.parameters ??= this.BindParameters(this.DeclaringCompilation!.GlobalDiagnosticBag);
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType =>
        this.returnType ??= this.BindReturnType(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
    private TypeSymbol? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override FunctionDeclarationSyntax DeclaringSyntax { get; }

    public BoundStatement Body =>
        this.body ??= this.BindBody(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
    private BoundStatement? body;

    public override string Documentation => this.DeclaringSyntax.Documentation;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    public void Bind(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        this.BindParameters(diagnostics);
        this.BindReturnType(binderProvider, diagnostics);
        this.BindBody(binderProvider, diagnostics);

        // Flow analysis
        ReturnsOnAllPaths.Analyze(this, diagnostics);
        DefiniteAssignment.Analyze(this.Body, diagnostics);
        ValAssignment.Analyze(this, diagnostics);
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

    private TypeSymbol BindReturnType(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclaringSyntax.ReturnType is null) return IntrinsicSymbols.Unit;

        // Otherwise, we need to resolve
        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        return (TypeSymbol)binder.BindType(this.DeclaringSyntax.ReturnType.Type, diagnostics);
    }

    private BoundStatement BindBody(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Body);
        return binder.BindFunction(this, diagnostics);
    }
}
