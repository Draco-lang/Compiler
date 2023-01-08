using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis.Lattices;

// TODO: Decommission, not needed anymore
/// <summary>
/// A lattice for checking, if the function returns on all paths.
/// </summary>
internal sealed class ReturnsOnAllPaths : ILattice<ReturnsOnAllPaths.Status>
{
    public enum Status
    {
        DoesNotReturn = 0,
        Returns = 1,
    }

    public static ReturnsOnAllPaths Instance { get; } = new();

    public FlowDirection Direction => FlowDirection.Forward;
    public Status Identity => Status.DoesNotReturn;

    private ReturnsOnAllPaths()
    {
    }

    public bool Equals(Status x, Status y) => x == y;
    public int GetHashCode(Status obj) => obj.GetHashCode();

    public Status Join(Status a, Status b) => (Status)Math.Max((int)a, (int)b);
    public Status Meet(Status a, Status b) => (Status)Math.Min((int)a, (int)b);

    public Status Transfer(Ast node) => node is Ast.Expr.Return
        ? Status.Returns
        : Status.DoesNotReturn;
}
