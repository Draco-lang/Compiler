using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
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
    protected UntypedStatement BindStatement(SyntaxNode syntax, ConstraintBag constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedFunctionBodySyntax or UnexpectedStatementSyntax => new UntypedUnexpectedStatement(syntax),
        // Function declarations get discarded, as they become symbols
        // TODO: Actually make local functions work by making them symbols
        FunctionDeclarationSyntax func => UntypedNoOpStatement.Default,
        DeclarationStatementSyntax decl => this.BindStatement(decl.Declaration, constraints, diagnostics),
        ExpressionStatementSyntax expr => this.BindExpressionStatement(expr, constraints, diagnostics),
        BlockFunctionBodySyntax body => this.BindBlockFunctionBody(body, constraints, diagnostics),
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints, diagnostics),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints, diagnostics),
        VariableDeclarationSyntax decl => this.BindVariableDeclaration(decl, constraints, diagnostics),
        _ => throw new System.ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedStatement BindExpressionStatement(ExpressionStatementSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var expr = this.BindExpression(syntax.Expression, constraints, diagnostics);
        return new UntypedExpressionStatement(syntax, expr);
    }

    private UntypedStatement BindBlockFunctionBody(BlockFunctionBodySyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics));
        // TODO: Do we want to handle this here, or during DFA?
        // If this function returns unit, we implicitly append a return expression
        var function = (FunctionSymbol)this.ContainingSymbol!;
        if (ReferenceEquals(function.ReturnType, Types.Intrinsics.Unit))
        {
            statements = statements
                .Append(new UntypedExpressionStatement(
                    syntax: null,
                    expression: new UntypedReturnExpression(
                        syntax: null,
                        value: UntypedUnitExpression.Default)));
        }
        return new UntypedExpressionStatement(
            syntax,
            new UntypedBlockExpression(syntax, locals, statements.ToImmutableArray(), UntypedUnitExpression.Default));
    }

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var value = binder.BindExpression(syntax.Value, constraints, diagnostics);

        // Constraint return type
        var containingFunction = (FunctionSymbol?)this.ContainingSymbol;
        Debug.Assert(containingFunction is not null);
        constraints.Return(value, containingFunction, syntax);

        return new UntypedExpressionStatement(syntax, new UntypedReturnExpression(syntax.Value, value));
    }

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var labelSymbol = (LabelSymbol?)this.DeclaredSymbols
            .OfType<LabelSymbol>()
            .OfType<ISourceSymbol>()
            .FirstOrDefault(sym => sym.DeclarationSyntax == syntax);
        Debug.Assert(labelSymbol is not null);

        return new UntypedLabelStatement(syntax, labelSymbol);
    }

    private UntypedStatement BindVariableDeclaration(VariableDeclarationSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var localSymbol = (UntypedLocalSymbol?)this.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .OfType<ISourceSymbol>()
            .FirstOrDefault(sym => sym.DeclarationSyntax == syntax);
        Debug.Assert(localSymbol is not null);

        var type = syntax.Type is null ? null : this.BindType(syntax.Type.Type, diagnostics);
        var value = syntax.Value is null ? null : this.BindExpression(syntax.Value.Value, constraints, diagnostics);

        constraints.LocalDeclaration(localSymbol, type, value, syntax);

        return new UntypedLocalDeclaration(syntax, localSymbol, value);
    }
}
