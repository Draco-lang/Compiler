using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Types;
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
        var binder = this.Compilation.GetBinder(syntax);
        var statements = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics))
            .ToImmutableArray();
        return new UntypedExpressionStatement(
            syntax,
            new UntypedBlockExpression(syntax, statements, UntypedUnitExpression.Default));
    }

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var binder = this.Compilation.GetBinder(syntax);
        var value = binder.BindExpression(syntax.Value, constraints, diagnostics);
        return new UntypedExpressionStatement(syntax, new UntypedReturnExpression(syntax.Value, value));
    }

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var labelSymbol = (LabelSymbol?)((LocalBinder)this).Declarations
            .OfType<ISourceSymbol>()
            .FirstOrDefault(sym => sym.DeclarationSyntax == syntax);
        Debug.Assert(labelSymbol is not null);

        return new UntypedLabelStatement(syntax, labelSymbol);
    }

    private UntypedStatement BindVariableDeclaration(VariableDeclarationSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        // Look up the corresponding symbol defined
        var localSymbol = (LocalSymbol?)((LocalBinder)this).LocalDeclarations
            .Select(decl => decl.Symbol)
            .OfType<ISourceSymbol>()
            .FirstOrDefault(sym => sym.DeclarationSyntax == syntax);
        Debug.Assert(localSymbol is not null);

        var type = syntax.Type is null ? null : this.BindType(syntax.Type.Type, constraints, diagnostics);
        var value = syntax.Value is null ? null : this.BindExpression(syntax.Value, constraints, diagnostics);

        // TODO: If type not null, constraint that variable has exact type
        // TODO: If value not null, constraint that its assignable to variable type

        return new UntypedLocalDeclaration(syntax, localSymbol, value);
    }
}
