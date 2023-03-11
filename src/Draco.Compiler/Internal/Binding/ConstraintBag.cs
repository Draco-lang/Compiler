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
    public void LocalDeclaration(LocalSymbol symbol, Symbol type, SyntaxNode syntax) => throw new System.NotImplementedException();
    internal void LocalAssignment(LocalSymbol symbol, UntypedExpression value, SyntaxNode syntax) => throw new System.NotImplementedException();
    public Type LocalReference(LocalSymbol local, SyntaxNode syntax) => throw new System.NotImplementedException();
    public void HasType(UntypedExpression expr, Type type) => throw new System.NotImplementedException();
    public void IsAssignable(UntypedLvalue left, UntypedExpression right) => throw new System.NotImplementedException();
    public Type CommonType(UntypedExpression first, UntypedExpression second) => throw new System.NotImplementedException();
    public Type CallUnaryOperator(Symbol @operator, UntypedExpression operand) => throw new System.NotImplementedException();
    public Type CallBinaryOperator(Symbol @operator, UntypedExpression left, UntypedExpression right) => throw new System.NotImplementedException();
    public Type CallComparisonOperator(Symbol @operator, UntypedExpression left, UntypedExpression right) => throw new System.NotImplementedException();
}
