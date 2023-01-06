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

internal sealed class ReturnsOnAllPaths : LatticeBase<ReturnStatus>
{
    public override FlowDirection Direction => FlowDirection.Forward;
    public override ReturnStatus Identity => ReturnStatus.DoesNotReturn;

    public override bool Equals(ReturnStatus x, ReturnStatus y) => x == y;
    public override int GetHashCode(ReturnStatus obj) => obj.GetHashCode();
    public override ReturnStatus Clone(ReturnStatus element) => element;

    public override void Meet(ref ReturnStatus result, ReturnStatus input) =>
        result = (int)result < (int)input ? result : input;

    public override void Transfer(ref ReturnStatus element, Ast.Expr.Return node) =>
        element = ReturnStatus.Returns;
}
