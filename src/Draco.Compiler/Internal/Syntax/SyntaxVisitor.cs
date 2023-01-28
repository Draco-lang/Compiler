using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax;

internal abstract partial class SyntaxVisitor
{
    public virtual void VisitSyntaxToken(SyntaxToken node) { }
}

internal abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult VisitSyntaxToken(SyntaxToken node) => default!;
}
