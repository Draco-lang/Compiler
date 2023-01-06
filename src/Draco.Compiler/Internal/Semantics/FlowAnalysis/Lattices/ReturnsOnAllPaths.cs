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

    public override ReturnStatus Clone(ReturnStatus element) => element;

    public override bool Meet(ref ReturnStatus result, ReturnStatus input)
    {
        var oldResult = result;
        result = (int)result < (int)input ? result : input;
        return result != oldResult;
    }

    public override bool Join(ref ReturnStatus element, Ast.Expr.Return node)
    {
        var oldElement = element;
        element = ReturnStatus.Returns;
        return element != oldElement;
    }
}
