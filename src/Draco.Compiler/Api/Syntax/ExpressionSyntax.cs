using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;
public partial class ExpressionSyntax
{
    public int ArgumentIndex
    {
        get
        {
            if (this.Parent is not CallExpressionSyntax callExpression) return -1;
            if (this == callExpression.Function) return -1;
            foreach (var (item, i) in callExpression.ArgumentList.Values.Select((s, i) => (s, i)))
            {
                if (item == this) return i;
            }
            throw new InvalidOperationException();
        }
    }
}
