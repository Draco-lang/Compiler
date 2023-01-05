using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.Semantics.FlowAnalysis;

namespace Draco.Compiler.Internal.Semantics.AbstractSyntax;

/// <summary>
/// Utility to represent <see cref="FlowOperation"/>s as a <see cref="IControlFlowGraph{TStatement}"/>.
/// </summary>
internal static class FlowOperationsToCfg
{
    public static IControlFlowGraph<FlowOperation> ToCfg(BasicBlock procedure) =>
        throw new NotImplementedException();
}
