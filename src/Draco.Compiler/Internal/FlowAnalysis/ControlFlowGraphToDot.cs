using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.FlowAnalysis;

/// <summary>
/// Utility to convert a control flow graph to a DOT graph.
/// </summary>
internal static class ControlFlowGraphToDot
{
    /// <summary>
    /// Translates the control flow graph to a DOT graph.
    /// </summary>
    /// <param name="cfg">The control flow graph to convert.</param>
    /// <returns>The DOT representation of the control flow graph.</returns>
    public static string ToDot(this IControlFlowGraph cfg) =>
        ToDotIntenral(cfg, null as DataFlowAnalysis<int>);

    /// <summary>
    /// Translates the control flow graph to a DOT graph with flow analysis information.
    /// </summary>
    /// <typeparam name="TState">The state type of the domain used in the flow analysis.</typeparam>
    /// <param name="cfg">The control flow graph to convert.</param>
    /// <param name="flowAnalysis">The flow analysis to use for additional information.</param>
    /// <returns>The DOT representation of the control flow graph.</returns>
    public static string ToDot<TState>(this IControlFlowGraph cfg, DataFlowAnalysis<TState> flowAnalysis) =>
        ToDotIntenral(cfg, flowAnalysis);

    private static string ToDotIntenral<TState>(IControlFlowGraph cfg, DataFlowAnalysis<TState>? flowAnalysis = null)
    {
        var blockNames = new Dictionary<IBasicBlock, string>();
        var boundNodeNames = new Dictionary<BoundNode, string>();

        var graph = new DotGraphBuilder<IBasicBlock>(isDirected: true);
        graph.WithName("ControlFlowGraph");

        // We name the blocks using a breadth-first manner to have some kind of logical ranking
        var blocksInDepthFirstOrder = GraphTraversal.BreadthFirst(
            cfg.Entry,
            bb => bb.Successors
                .Select(s => s.Successor)
                .Concat(bb.Predecessors.Select(s => s.Predecessor)));
        foreach (var block in blocksInDepthFirstOrder) blockNames[block] = $"b{blockNames.Count}";

        foreach (var block in cfg.AllBlocks)
        {
            graph
                .AddVertex(block)
                .WithShape(DotAttribs.Shape.Rectangle)
                .WithHtmlAttribute("label", BasicBlockToLabel(block))
                .WithXLabel(blockNames[block]);

            foreach (var edge in block.Successors)
            {
                graph
                    .AddEdge(block, edge.Successor)
                    .WithLabel(edge.ToString());
            }
            // NOTE: Adding the predecessor would just cause each edge to show up twice
        }

        return graph.ToDot();

        string BasicBlockToLabel(IBasicBlock block)
        {
            var result = new StringBuilder();
            // Append the entry state of the block at the start
            if (flowAnalysis is not null && block.Count > 0)
            {
                var entryState = flowAnalysis.GetEntry(block);
                var str = flowAnalysis.Domain.ToString(entryState);
                result.Append(str);
                result.Append("<br align=\"center\"/>");
            }
            // Print each node in the block
            foreach (var node in block)
            {
                result.Append(BoundNodeToLabel(node));
                result.Append("<br align=\"left\"/>");
                // Append the state after the node
                if (flowAnalysis is not null)
                {
                    var exitState = flowAnalysis.GetExit(node);
                    var str = flowAnalysis.Domain.ToString(exitState);
                    result.Append(str);
                    result.Append("<br align=\"center\"/>");
                }
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

        // For stuff like literals we don't need to allocate a new name, just pretty-print contents
        static string? GetSimplifiedBoundNodeLabel(BoundNode node) => node switch
        {
            BoundUnitExpression => "unit",
            BoundParameterExpression param => param.Parameter.Name,
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
