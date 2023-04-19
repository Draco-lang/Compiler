using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Declarations;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An in-source function-definition.
/// </summary>
internal sealed class SourceFunctionSymbol : FunctionSymbol
{
    public override ImmutableArray<ParameterSymbol> Parameters => this.parameters ??= this.BuildParameters();
    private ImmutableArray<ParameterSymbol>? parameters;

    public override TypeSymbol ReturnType => this.returnType ??= this.BuildReturnType();
    private TypeSymbol? returnType;

    public override Symbol? ContainingSymbol { get; }
    public override string Name => this.DeclarationSyntax.Name.Text;

    public override FunctionDeclarationSyntax DeclarationSyntax { get; }

    public BoundStatement Body => this.body ??= this.BuildBody();
    private BoundStatement? body;

    public override string Documentation => this.DeclarationSyntax.Documentation;

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclarationSyntax syntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclarationSyntax = syntax;
    }

    public SourceFunctionSymbol(Symbol? containingSymbol, FunctionDeclaration declaration)
        : this(containingSymbol, declaration.Syntax)
    {
    }

    private ImmutableArray<ParameterSymbol> BuildParameters()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;

        var parameterSyntaxes = this.DeclarationSyntax.ParameterList.Values.ToList();
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

    private TypeSymbol BuildReturnType()
    {
        // If the return type is unspecified, it's assumed to be unit
        if (this.DeclarationSyntax.ReturnType is null) return IntrinsicSymbols.Unit;

        // Otherwise, we need to resolve
        Debug.Assert(this.DeclaringCompilation is not null);

        // NOTE: We are using the global diagnostic bag, maybe that's not a good idea here?
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax);
        return (TypeSymbol)binder.BindType(this.DeclarationSyntax.ReturnType.Type, diagnostics);
    }

    private BoundStatement BuildBody()
    {
        Debug.Assert(this.DeclaringCompilation is not null);
        var binder = this.DeclaringCompilation.GetBinder(this.DeclarationSyntax.Body);
        var diagnostics = this.DeclaringCompilation.GlobalDiagnosticBag;
        return binder.BindFunction(this, diagnostics);
    }
}
