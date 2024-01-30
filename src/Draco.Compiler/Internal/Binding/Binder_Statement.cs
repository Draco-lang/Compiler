using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped statement.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped statement for <paramref name="syntax"/>.</returns>
    protected virtual BindingTask<BoundStatement> BindStatement(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedFunctionBodySyntax or UnexpectedStatementSyntax => FromResult(new BoundUnexpectedStatement(syntax)),
        // Ignored
        ImportDeclarationSyntax => FromResult(BoundNoOpStatement.Default),
        FunctionDeclarationSyntax func => this.BindFunctionDeclaration(func, constraints, diagnostics),
        DeclarationStatementSyntax decl => this.BindStatement(decl.Declaration, constraints, diagnostics),
        ExpressionStatementSyntax expr => this.BindExpressionStatement(expr, constraints, diagnostics),
        BlockFunctionBodySyntax body => this.BindBlockFunctionBody(body, constraints, diagnostics),
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints, diagnostics),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints, diagnostics),
        VariableDeclarationSyntax decl => this.BindVariableDeclaration(decl, constraints, diagnostics),
        _ => throw new System.ArgumentOutOfRangeException(nameof(syntax)),
    };

    private static BindingTask<BoundStatement> FromResult(BoundStatement stmt) => BindingTask.FromResult(stmt);

    private BindingTask<BoundStatement> BindFunctionDeclaration(FunctionDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.DeclaredSymbols
            .OfType<SourceFunctionSymbol>()
            .First(s => s.DeclaringSyntax == syntax);
        return FromResult(new BoundLocalFunction(syntax, symbol));
    }

    private async BindingTask<BoundStatement> BindExpressionStatement(ExpressionStatementSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var exprTask = this.BindExpression(syntax.Expression, constraints, diagnostics);
        _ = exprTask.GetResultType(syntax.Expression, constraints, diagnostics);
        return new BoundExpressionStatement(syntax, await exprTask);
    }

    private async BindingTask<BoundStatement> BindBlockFunctionBody(BlockFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<LocalSymbol>()
            .ToImmutableArray();
        var statementsTask = new List<BindingTask<BoundStatement>>();
        statementsTask.AddRange(syntax.Statements.Select(s => binder.BindStatement(s, constraints, diagnostics)));
        // TODO: Do we want to handle this here, or during DFA?
        // If this function returns unit, we implicitly append a return expression
        var function = (FunctionSymbol)this.ContainingSymbol!;
        if (SymbolEqualityComparer.Default.Equals(function.ReturnType, WellKnownTypes.Unit))
        {
            statementsTask.Add(FromResult(new BoundExpressionStatement(
                syntax: null,
                expression: new BoundReturnExpression(
                    syntax: null,
                    value: BoundUnitExpression.Default))));
        }
        return new BoundExpressionStatement(
            syntax,
            new BoundBlockExpression(
                syntax,
                locals,
                await BindingTask.WhenAll(statementsTask),
                BoundUnitExpression.Default));
    }

    private async BindingTask<BoundStatement> BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var valueTask = binder.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax.Value, valueTask, constraints, diagnostics);

        return new BoundExpressionStatement(syntax, new BoundReturnExpression(syntax, await valueTask));
    }

    private BindingTask<BoundStatement> BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var labelSymbol = this.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.DeclaringSyntax == syntax);

        return FromResult(new BoundLabelStatement(syntax, labelSymbol));
    }

    private async BindingTask<BoundStatement> BindVariableDeclaration(VariableDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var localSymbol = this.DeclaredSymbols
            .OfType<LocalSymbol>()
            .First(sym => sym.DeclaringSyntax == syntax);

        var type = syntax.Type is null ? null : this.BindTypeToTypeSymbol(syntax.Type.Type, diagnostics);
        var valueTask = syntax.Value is null ? null : this.BindExpression(syntax.Value.Value, constraints, diagnostics);

        constraints.DeclareLocal(localSymbol);
        if (type is not null) ConstraintSolver.UnifyAsserted(localSymbol.Type, type);

        if (valueTask is not null)
        {
            // It has to be assignable
            _ = constraints.Assignable(
                localSymbol.Type,
                valueTask.GetResultType(syntax.Value, constraints, diagnostics),
                syntax.Value!.Value);
        }

        return new BoundLocalDeclaration(syntax, localSymbol, valueTask is null ? null : await valueTask);
    }
}
