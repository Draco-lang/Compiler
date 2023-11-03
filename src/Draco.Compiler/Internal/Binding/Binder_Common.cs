using System.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    protected void ConstraintReturnType(SyntaxNode returnSyntax, UntypedExpression returnValue, ConstraintSolver constraints)
    {
        var containingFunction = (FunctionSymbol?)this.ContainingSymbol;
        Debug.Assert(containingFunction is not null);
        var returnTypeSyntax = (containingFunction as SourceFunctionSymbol)?.DeclaringSyntax?.ReturnType?.Type;
        constraints.Assignable(
            containingFunction.ReturnType,
            returnValue.TypeRequired,
            ConstraintLocator.Syntax(returnSyntax)
                .WithRelatedInformation(
                    format: "return type declared to be {0}",
                    formatArgs: containingFunction.ReturnType,
                    location: returnTypeSyntax?.Location));
    }
}
