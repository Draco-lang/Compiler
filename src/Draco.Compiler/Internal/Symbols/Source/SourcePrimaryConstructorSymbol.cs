using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// Primary constructor defined in-source.
/// </summary>
internal sealed class SourcePrimaryConstructorSymbol : FunctionSymbol, ISourceSymbol
{
    public override TypeSymbol ContainingSymbol { get; }

    public override string Name => ".ctor";
    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;
    public override bool IsStatic => false;
    public override bool IsSpecialName => true;
    public override bool IsConstructor => true;
    public override Api.Semantics.Visibility Visibility => GetVisibilityFromTokenKind(this.DeclaringSyntax.VisibilityModifier?.Kind);

    public override ImmutableArray<ParameterSymbol> Parameters => this.BindParametersIfNeeded(this.DeclaringCompilation!);
    private ImmutableArray<ParameterSymbol> parameters;

    public override PrimaryConstructorSyntax DeclaringSyntax { get; }

    public override BoundStatement Body => InterlockedUtils.InitializeNull(ref this.body, this.BuildBody);
    private BoundStatement? body;

    public SourcePrimaryConstructorSymbol(TypeSymbol containingSymbol, PrimaryConstructorSyntax declaringSyntax)
    {
        this.ContainingSymbol = containingSymbol;
        this.DeclaringSyntax = declaringSyntax;
    }

    public void Bind(IBinderProvider binderProvider)
    {
        this.BindParametersIfNeeded(binderProvider);
        // Force binding of parameters, as the type is lazy too
        foreach (var param in this.Parameters.Cast<SourceParameterSymbol>()) param.Bind(binderProvider);
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

    private BoundStatement BuildBody()
    {
        var thisSymbol = new SynthetizedThisParameterSymbol(this);

        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        foreach (var (paramSyntax, paramSymbol) in this.DeclaringSyntax.ParameterList.Values.Zip(this.Parameters))
        {
            // Skip non-members
            if (paramSyntax.MemberModifiers is null) continue;

            var isField = paramSyntax.MemberModifiers.FieldModifier is not null;
            var name = paramSyntax.Parameter.Name.Text;

            // Search for the field or property
            // NOTE: There should _not_ be an error here, the symbols should already be present
            var memberSymbol = this.ContainingSymbol.DefinedMembers
                .Where(x => isField ? x is FieldSymbol : x is PropertySymbol)
                .First(m => m.Name == name);

            // Build the initializer
            var initializer = ExpressionStatement(AssignmentExpression(
                compoundOperator: null,
                // TODO: Handle properties
                left: FieldLvalue(
                    receiver: ParameterExpression(thisSymbol),
                    field: (FieldSymbol)memberSymbol),
                right: ParameterExpression(paramSymbol)));

            // Add it
            statements.Add(initializer);
        }

        // Add a return statement
        statements.Add(ExpressionStatement(ReturnExpression(BoundUnitExpression.Default)));

        return ExpressionStatement(BlockExpression(
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: statements.ToImmutable(),
            value: BoundUnitExpression.Default));
    }
}
