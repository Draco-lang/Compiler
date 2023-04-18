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
    public override ImmutableArray<ParameterSymbol> Parameters => this.BindParameters(this.DeclaringCompilation!.GlobalDiagnosticBag);
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType => this.BindReturnType(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
    private TypeSymbol? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclaringSyntax.Name.Text;

    public override FunctionDeclarationSyntax DeclaringSyntax { get; }

    public BoundStatement Body => this.BindBody(this.DeclaringCompilation!, this.DeclaringCompilation!.GlobalDiagnosticBag);
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
        if (this.parameters is not null) return this.parameters.Value;

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

        this.parameters = parameters.ToImmutable();
        return this.parameters.Value;
    }

    private TypeSymbol BindReturnType(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        if (this.returnType is not null) return this.returnType;

        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclaringSyntax.ReturnType is null) return IntrinsicSymbols.Unit;

        // Otherwise, we need to resolve
        Debug.Assert(this.DeclaringCompilation is not null);

        var binder = binderProvider.GetBinder(this.DeclaringSyntax);
        this.returnType = (TypeSymbol)binder.BindType(this.DeclaringSyntax.ReturnType.Type, diagnostics);

        return this.returnType;
    }

    private BoundStatement BindBody(IBinderProvider binderProvider, DiagnosticBag diagnostics)
    {
        if (this.body is not null) return this.body;

        var binder = binderProvider.GetBinder(this.DeclaringSyntax.Body);
        this.body = binder.BindFunction(this, diagnostics);

        return this.body;
    }
}
