using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a set of constraints from the typesystem.
/// </summary>
internal sealed class ConstraintBag
{
    public Type LocalReference(LocalSymbol local, NameExpressionSyntax syntax) => throw new System.NotImplementedException();
    public void HasType(UntypedExpression condition, Types.Type @bool) => throw new System.NotImplementedException();
    public void IsAssignable(UntypedLvalue left, UntypedExpression right) => throw new System.NotImplementedException();
    public Type CommonType(UntypedExpression then, UntypedExpression @else) => throw new System.NotImplementedException();
    public Type CallUnaryOperator(Symbol operatorSymbol, UntypedExpression operand) => throw new System.NotImplementedException();
    public Type CallBinaryOperator(Symbol operatorSymbol, UntypedExpression left, UntypedExpression right) => throw new System.NotImplementedException();
    public Type CallComparisonOperator(Symbol operatorSymbol, UntypedExpression prev, UntypedExpression right) => throw new System.NotImplementedException();
}
