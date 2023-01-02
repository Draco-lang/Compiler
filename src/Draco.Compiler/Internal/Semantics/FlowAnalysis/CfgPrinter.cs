using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.FlowAnalysis;

/// <summary>
/// Printting utilities for <see cref="IControlFlowGraph{TStatement}"/>s.
/// </summary>
internal static class CfgPrinter
{
    /// <summary>
    /// Converts the given CFG to a dot-graph representation.
    /// </summary>
    /// <typeparam name="TStatement">The statement type.</typeparam>
    /// <param name="cfg">The CFG to convert.</param>
    /// <param name="bbToString">The function to stringify the basic-blocks.</param>
    /// <returns>The CFG as a DOT graph.</returns>
    public static string ToDot<TStatement>(
        IControlFlowGraph<TStatement> cfg,
        Func<IBasicBlock<TStatement>, string>? bbToString = null)
    {
        var bbIds = new Dictionary<IBasicBlock<TStatement>, string>();
        bbToString ??= bb => bbIds[bb];

        string GetBlockName(IBasicBlock<TStatement> block)
        {
            if (!bbIds!.TryGetValue(block, out var name))
            {
                name = $"bb_{StringUtils.IndexToExcelColumnName(bbIds.Count)}";
                bbIds.Add(block, name);
            }
            return name;
        }

        var result = new StringBuilder();
        result.AppendLine("digraph CFG {");
        // Declare blocks
        foreach (var block in cfg.Blocks)
        {
            result.AppendLine($"    {GetBlockName(block)} [label=\"{StringUtils.Unescape(bbToString(block))}\"];");
        }
        // Entry
        result.AppendLine($"    {GetBlockName(cfg.Entry)} [xlabel=\"ENTRY\"];");
        // Exit
        foreach (var exit in cfg.Exit) result.AppendLine($"    {GetBlockName(exit)} [xlabel=\"EXIT\"];");
        // Connect them up
        foreach (var block in cfg.Blocks)
        {
            foreach (var succ in block.Successors) result.AppendLine($"    {GetBlockName(block)} -> {GetBlockName(succ)};");
        }
        result.AppendLine("}");
        return result.ToString();
    }
}
