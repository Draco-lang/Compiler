using System;
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
        UnexpectedFunctionBodySyntax or UnexpectedStatementSyntax => FromResult(constraints, new BoundUnexpectedStatement(syntax)),
        // Ignored
        ImportDeclarationSyntax => FromResult(constraints, BoundNoOpStatement.Default),
        FunctionDeclarationSyntax func => this.BindFunctionDeclaration(func, constraints, diagnostics),
        DeclarationStatementSyntax decl => this.BindStatement(decl.Declaration, constraints, diagnostics),
        ExpressionStatementSyntax expr => this.BindExpressionStatement(expr, constraints, diagnostics),
        BlockFunctionBodySyntax body => this.BindBlockFunctionBody(body, constraints, diagnostics),
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints, diagnostics),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints, diagnostics),
        VariableDeclarationSyntax decl => this.BindVariableDeclaration(decl, constraints, diagnostics),
        _ => throw new System.ArgumentOutOfRangeException(nameof(syntax)),
    };

    private static BindingTask<BoundStatement> FromResult(ConstraintSolver constraints, BoundStatement stmt) =>
        BindingTask.FromResult(constraints, stmt);

    private BindingTask<BoundStatement> BindFunctionDeclaration(FunctionDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.DeclaredSymbols
            .OfType<SourceFunctionSymbol>()
            .First(s => s.DeclaringSyntax == syntax);
        return FromResult(constraints, new BoundLocalFunction(syntax, symbol));
    }

    private async BindingTask<BoundStatement> BindExpressionStatement(ExpressionStatementSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var exprTask = this.BindExpression(syntax.Expression, constraints, diagnostics);
        return new BoundExpressionStatement(syntax, await exprTask);
    }

    private BindingTask<BoundStatement> BindBlockFunctionBody(BlockFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = ImmutableArray.CreateBuilder<BoundStatement>();
        statements.AddRange(syntax.Statements.Select(s => binder.BindStatement(s, constraints, diagnostics)));
        // TODO: Do we want to handle this here, or during DFA?
        // If this function returns unit, we implicitly append a return expression
        var function = (FunctionSymbol)this.ContainingSymbol!;
        if (SymbolEqualityComparer.Default.Equals(function.ReturnType, IntrinsicSymbols.Unit))
        {
            statements.Add(new BoundExpressionStatement(
                syntax: null,
                expression: new BoundReturnExpression(
                    syntax: null,
                    value: BoundUnitExpression.Default)));
        }
        return new BoundExpressionStatement(
            syntax,
            new BoundBlockExpression(syntax, locals, statements.ToImmutable(), BoundUnitExpression.Default));
#else
        throw new NotImplementedException();
#endif
    }

    private async BindingTask<BoundStatement> BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var valueTask = binder.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax.Value, valueTask, constraints);

        return new BoundExpressionStatement(syntax, new BoundReturnExpression(syntax, await valueTask));
    }

    private BindingTask<BoundStatement> BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var labelSymbol = this.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.DeclaringSyntax == syntax);

        return FromResult(constraints, new BoundLabelStatement(syntax, labelSymbol));
    }

    private BindingTask<BoundStatement> BindVariableDeclaration(VariableDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        // Look up the corresponding symbol defined
        var localSymbol = this.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .First(sym => sym.DeclaringSyntax == syntax);

        var type = syntax.Type is null ? null : this.BindTypeToTypeSymbol(syntax.Type.Type, diagnostics);
        var value = syntax.Value is null ? null : this.BindExpression(syntax.Value.Value, constraints, diagnostics);

        var declaredType = constraints.DeclareLocal(localSymbol, type);
        if (value is not null)
        {
            // It has to be assignable
            constraints.Assignable(declaredType, value.TypeRequired, syntax.Value!.Value);
        }

        return new UntypedLocalDeclaration(syntax, localSymbol, value);
#else
        throw new NotImplementedException();
#endif
    }
}
