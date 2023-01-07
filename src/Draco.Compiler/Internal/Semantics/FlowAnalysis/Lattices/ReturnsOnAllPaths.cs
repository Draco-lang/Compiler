using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;

internal enum ReturnStatus
{
    DoesNotReturn = 0,
    Returns = 1,
}

internal sealed class ReturnsOnAllPaths : ILattice<ReturnStatus>
{
    public FlowDirection Direction => FlowDirection.Forward;
    public ReturnStatus Identity => ReturnStatus.DoesNotReturn;

    public bool Equals(ReturnStatus x, ReturnStatus y) => x == y;
    public int GetHashCode(ReturnStatus obj) => obj.GetHashCode();

    public ReturnStatus Join(ReturnStatus a, ReturnStatus b) => (ReturnStatus)Math.Max((int)a, (int)b);
    public ReturnStatus Meet(ReturnStatus a, ReturnStatus b) => (ReturnStatus)Math.Min((int)a, (int)b);

    public ReturnStatus Transfer(Ast node) => node is Ast.Expr.Return
        ? ReturnStatus.Returns
        : ReturnStatus.DoesNotReturn;
}
