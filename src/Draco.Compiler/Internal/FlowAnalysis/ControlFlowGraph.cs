using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// A control flow graph consisting of <see cref="IBasicBlock"/>s.
/// </summary>
internal interface IControlFlowGraph
{
    /// <summary>
    /// The entry point of the control flow graph.
    /// </summary>
    public IBasicBlock Entry { get; }

    /// <summary>
    /// All basic blocks in the control flow graph.
    /// </summary>
    public IEnumerable<IBasicBlock> AllBlocks { get; }

    /// <summary>
    /// Translates the control flow graph to a DOT graph.
    /// </summary>
    /// <returns>The DOT representation of the control flow graph.</returns>
    public string ToDot();
}

/// <summary>
/// An implementation of <see cref="IControlFlowGraph"/>.
/// </summary>
internal sealed class ControlFlowGraph(BasicBlock entry) : IControlFlowGraph
{
    public BasicBlock Entry { get; } = entry;
    IBasicBlock IControlFlowGraph.Entry => this.Entry;

    public IEnumerable<IBasicBlock> AllBlocks => GraphTraversal.DepthFirst(
        start: (this as IControlFlowGraph).Entry,
        getNeighbors: n => n.Successors.Select(e => e.Successor).Concat(n.Predecessors.Select(e => e.Predecessor)));

    public string ToDot()
    {
        var boundNodeNames = new Dictionary<BoundNode, string>();

        var graph = new DotGraphBuilder<IBasicBlock>(isDirected: true);
        graph.WithName("ControlFlowGraph");

        foreach (var block in this.AllBlocks)
        {
            graph
                .AddVertex(block)
                .WithShape(DotAttribs.Shape.Rectangle)
                .WithHtmlAttribute("label", BasicBlockToLabel(block));

            foreach (var edge in block.Successors)
            {
                graph
                    .AddEdge(block, edge.Successor)
                    .WithLabel(EdgeToLabel(edge));
            }
            // NOTE: Adding the predecessor would just cause each edge to show up twice
        }

        return graph.ToDot();

        string BasicBlockToLabel(IBasicBlock block)
        {
            var result = new StringBuilder();
            foreach (var node in block)
            {
                result.Append(BoundNodeToLabel(node));
                result.Append("<br align=\"left\"/>");
            }
            // NOTE: Empty HTML labels are not allowed by Graphviz, we just add a space
            if (result.Length == 0) result.Append(' ');
            return result.ToString();
        }

        // Bound node to label with named arguments
        string BoundNodeToLabel(BoundNode node)
        {
            var nodeType = node.GetType();
            var nodeName = nodeType.Name.Replace("Bound", string.Empty);
            var nodePrefix = GetBoundNodeName(node);
            nodePrefix = nodePrefix is null ? null : $"{nodePrefix}: ";
            var nodeProps = nodeType.GetProperties();
            var argLabels = nodeProps
                .Select(prop => ArgumentToLabel(node, prop))
                .OfType<string>()
                .ToList();
            var args = argLabels.Count > 0
                ? $"({string.Join(", ", argLabels)})"
                : string.Empty;
            return $"{nodePrefix}{nodeName}{args}";
        }

        // Argument with name and value, skips uninteresting properties
        string? ArgumentToLabel(BoundNode node, PropertyInfo propInfo)
        {
            if (propInfo.Name == nameof(BoundNode.Syntax)) return null;
            if (propInfo.Name == nameof(BoundExpression.Type)) return null;
            if (propInfo.Name == nameof(BoundExpression.TypeRequired)) return null;

            var value = propInfo.GetValue(node);
            if (value is null) return null;
            var valueLabel = ArgumentValueToLabel(value);
            return valueLabel is null
                ? null
                : $"{propInfo.Name} = {valueLabel}";
        }

        // Turning a value into a label, handling cases like bound nodes, symbols, and collections
        string? ArgumentValueToLabel(object? value)
        {
            if (value is BoundNode node)
            {
                var simpleName = GetSimplifiedBoundNodeLabel(node);
                if (simpleName is not null) return simpleName;
                var nodeName = GetBoundNodeName(node);
                if (nodeName is not null) return nodeName;
                return BoundNodeToLabel(node);
            }
            if (value is null) return string.Empty;
            if (value is string s) return s;
            if (value is Symbol symbol) return symbol.Name;
            if (value is IEnumerable enumerable) return $"[{string.Join(", ", enumerable.Cast<object?>().Select(ArgumentValueToLabel))}]";
            return value.ToString() ?? string.Empty;
        }

        // Unique identifier for each bound node that needs it
        string? GetBoundNodeName(BoundNode node)
        {
            if (!NeedsUniqueName(node)) return null;
            if (!boundNodeNames.TryGetValue(node, out var name))
            {
                name = $"e{boundNodeNames.Count}";
                boundNodeNames.Add(node, name);
            }
            return name;
        }

        // Edges need to have the condition value appended
        string EdgeToLabel(SuccessorEdge edge)
        {
            var kind = edge.Condition.Kind.ToString();
            var value = edge.Condition.Value is null
                ? string.Empty
                : $"({ArgumentValueToLabel(edge.Condition.Value)})";
            return $"{kind}{value}";
        }

        // For stuff like literals we don't need to allocate a new name, just pretty-print contents
        static string? GetSimplifiedBoundNodeLabel(BoundNode node) => node switch
        {
            BoundUnitExpression => "unit",
            BoundLocalExpression local => local.Local.Name,
            BoundLocalLvalue local => local.Local.Name,
            BoundGlobalExpression global => global.Global.Name,
            BoundGlobalLvalue global => global.Global.Name,
            BoundLiteralExpression lit => lit.Value?.ToString() ?? "null",
            BoundStringExpression str when str.Parts.Length == 1
                                        && str.Parts[0] is BoundStringText text => text.Text,
            _ => null,
        };

        // If the node needs a unique name
        static bool NeedsUniqueName(BoundNode node) => node is BoundExpression or BoundLvalue;
    }
}
