using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    protected void ConstraintReturnType(SyntaxNode returnSyntax, BindingTask<BoundExpression> returnValue, ConstraintSolver constraints)
    {
        var containingFunction = (FunctionSymbol?)this.ContainingSymbol;
        Debug.Assert(containingFunction is not null);
        var returnTypeSyntax = (containingFunction as SourceFunctionSymbol)?.DeclaringSyntax?.ReturnType?.Type;
        constraints.Assignable(
            containingFunction.ReturnType,
            returnValue.GetResultTypeRequired(constraints),
            ConstraintLocator.Syntax(returnSyntax)
                .WithRelatedInformation(
                    format: "return type declared to be {0}",
                    formatArgs: containingFunction.ReturnType,
                    location: returnTypeSyntax?.Location));
    }
}
