using System.Linq;

namespace Draco.Compiler.Api.Syntax;

public partial class ExpressionSyntax
{
    public int ArgumentIndex
    {
        get
        {
            if (this.Parent is not CallExpressionSyntax callExpression) return -1;
            if (this == callExpression.Function) return -1;
            return callExpression.ArgumentList.Values
                .Select((a, i) => (Argument: a, Index: i))
                .First(p => p.Argument == this)
                .Index;
        }
    }
}
