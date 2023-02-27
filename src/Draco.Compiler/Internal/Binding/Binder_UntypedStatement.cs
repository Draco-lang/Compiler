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
    /// <returns>The untyped statement for <paramref name="syntax"/>.</returns>
    protected UntypedStatement BindStatement(SyntaxNode syntax, ConstraintBag constraints) => syntax switch
    {
        InlineFunctionBodySyntax body => this.BindInlineFunctionBody(body, constraints),
        LabelDeclarationSyntax label => this.BindLabelStatement(label, constraints),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, ConstraintBag constraints)
    {
        var binder = this.Compilation.GetBinder(syntax);
        var value = binder.BindExpression(syntax.Value, constraints);
        return binder.BindInlineFunctionBody(syntax, value, constraints);
    }

    private UntypedStatement BindInlineFunctionBody(InlineFunctionBodySyntax syntax, UntypedExpression value, ConstraintBag constraints) =>
        new UntypedExpressionStatement(syntax, new UntypedReturnExpression(syntax, value));

    private UntypedStatement BindLabelStatement(LabelDeclarationSyntax syntax, ConstraintBag constraints) =>
        throw new NotImplementedException();
}
