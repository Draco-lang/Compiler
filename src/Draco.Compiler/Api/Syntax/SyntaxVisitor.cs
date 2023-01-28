using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Syntax;

public abstract partial class SyntaxVisitor
{
    public virtual void VisitSyntaxToken(SyntaxToken node) { }
}

public abstract partial class SyntaxVisitor<TResult>
{
    public virtual TResult VisitSyntaxToken(SyntaxToken node) => default!;
}
