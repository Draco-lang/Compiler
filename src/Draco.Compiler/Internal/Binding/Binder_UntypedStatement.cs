using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
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
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints, diagnostics),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics)
    {
        var binder = this.Compilation.GetBinder(syntax);
        var value = binder.BindExpression(syntax.Value, constraints, diagnostics);
        return binder.BindInlineFunctionBody(syntax, value, constraints, diagnostics);
    }

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, UntypedExpression value, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        new UntypedExpressionStatement(syntax, new UntypedReturnExpression(syntax, value));

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintBag constraints, DiagnosticBag diagnostics) =>
        throw new NotImplementedException();
}
