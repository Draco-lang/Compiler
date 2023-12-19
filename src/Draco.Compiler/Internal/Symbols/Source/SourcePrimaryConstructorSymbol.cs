using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Primary constructor defined in-source.
/// </summary>
internal sealed class SourcePrimaryConstructorSymbol : FunctionSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override string Name => ".ctor";
    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;
    public override bool IsStatic => false;
    public override bool IsSpecialName => true;
    public override bool IsConstructor => true;

    public override ImmutableArray<ParameterSymbol> Parameters => this.BindParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<ParameterSymbol> parameters;

    public override PrimaryConstructorSyntax DeclaringSyntax { get; }

    public SourcePrimaryConstructorSymbol(TypeSymbol containingSymbol, PrimaryConstructorSyntax declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = declaringSyntax;
    }

    private ImmutableArray<ParameterSymbol> BindParametersIfNeeded(IBinderProvider binderProvider) =>
        InterlockedUtils.InitializeDefault(ref this.parameters, () => this.BindParameters(binderProvider));

    // TODO: Copypaste from SourceFunctionSymbol
    private ImmutableArray<ParameterSymbol> BindParameters(IBinderProvider binderProvider)
    {
        var parameterSyntaxes = this.DeclaringSyntax.ParameterList.Values
            .Select(v => v.Parameter)
            .ToList();
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
}
