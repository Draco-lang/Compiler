using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Draco.Compiler.Internal.Semantics.AbstractSyntax;
using Draco.Compiler.Internal.Semantics.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Represents a single operation during DFA.
/// </summary>
internal sealed class DataFlowOperation
{
    // NOTE: Mutable because of cycles...
    /// <summary>
    /// The <see cref="Ast"/> node corresponding to the operation.
    /// </summary>
    public Ast Node { get; set; }

    /// <summary>
    /// The predecessor operations of this one.
    /// </summary>
    public ISet<DataFlowOperation> Predecessors { get; } = new HashSet<DataFlowOperation>();

    /// <summary>
    /// The successor operations of this one.
    /// </summary>
    public ISet<DataFlowOperation> Successors { get; } = new HashSet<DataFlowOperation>();

    public DataFlowOperation(Ast node)
    {
        this.Node = node;
    }

    public static void Join(DataFlowOperation first, DataFlowOperation second)
    {
        first.Successors.Add(second);
        second.Predecessors.Add(first);
    }
}
