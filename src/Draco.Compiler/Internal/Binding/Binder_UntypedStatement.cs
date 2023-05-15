using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;

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
    protected virtual UntypedStatement BindStatement(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedFunctionBodySyntax or UnexpectedStatementSyntax => new UntypedUnexpectedStatement(syntax),
        // Ignored
        ImportDeclarationSyntax => UntypedNoOpStatement.Default,
        FunctionDeclarationSyntax func => this.BindFunctionDeclaration(func, constraints, diagnostics),
        DeclarationStatementSyntax decl => this.BindStatement(decl.Declaration, constraints, diagnostics),
        ExpressionStatementSyntax expr => this.BindExpressionStatement(expr, constraints, diagnostics),
        BlockFunctionBodySyntax body => this.BindBlockFunctionBody(body, constraints, diagnostics),
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints, diagnostics),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints, diagnostics),
        VariableDeclarationSyntax decl => this.BindVariableDeclaration(decl, constraints, diagnostics),
        _ => throw new System.ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedStatement BindFunctionDeclaration(FunctionDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.DeclaredSymbols
            .OfType<SourceFunctionSymbol>()
            .First(s => s.DeclaringSyntax == syntax);
        return new UntypedLocalFunction(syntax, symbol);
    }

    private UntypedStatement BindExpressionStatement(ExpressionStatementSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var expr = this.BindExpression(syntax.Expression, constraints, diagnostics);
        return new UntypedExpressionStatement(syntax, expr);
    }

    private UntypedStatement BindBlockFunctionBody(BlockFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = ImmutableArray.CreateBuilder<UntypedStatement>();
        statements.AddRange(syntax.Statements.Select(s => binder.BindStatement(s, constraints, diagnostics)));
        // TODO: Do we want to handle this here, or during DFA?
        // If this function returns unit, we implicitly append a return expression
        var function = (FunctionSymbol)this.ContainingSymbol!;
        if (SymbolEqualityComparer.Default.Equals(function.ReturnType, IntrinsicSymbols.Unit))
        {
            statements.Add(new UntypedExpressionStatement(
                syntax: null,
                expression: new UntypedReturnExpression(
                    syntax: null,
                    value: UntypedUnitExpression.Default)));
        }
        return new UntypedExpressionStatement(
            syntax,
            new UntypedBlockExpression(syntax, locals, statements.ToImmutable(), UntypedUnitExpression.Default));
    }

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var value = binder.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax.Value, value, constraints);

        return new UntypedExpressionStatement(syntax, new UntypedReturnExpression(syntax.Value, value));
    }

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var labelSymbol = this.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.DeclaringSyntax == syntax);

        return new UntypedLabelStatement(syntax, labelSymbol);
    }

    private UntypedStatement BindVariableDeclaration(VariableDeclarationSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
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
            constraints
                .Assignable(declaredType, value.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Value!.Value.Location));
        }

        return new UntypedLocalDeclaration(syntax, localSymbol, value);
    }
}
