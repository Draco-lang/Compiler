using System;
using System.Collections.Generic;
using System.Text;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Internal.Utilities;

/// <summary>
/// Base class for printing the parse tree in a DOT graph format.
/// </summary>
internal abstract class DotGraphParseTreePrinterBase
{
    /// <summary>
    /// Describes what to do with a node.
    /// </summary>
    protected enum NodeAction
    {
        /// <summary>
        /// Print the node normally.
        /// </summary>
        Print,

        /// <summary>
        /// Skip the node but print children.
        /// </summary>
        Skip,

        /// <summary>
        /// Terminate printing the subtree, not printing this node.
        /// </summary>
        Terminate,

        /// <summary>
        /// Terminate printing the subtree, printing this node only instead.
        /// </summary>
        TerminateChildren,
    }

    protected string Code => $$"""
        digraph scope_tree {
          rankdir="BT"
          graph[ordering="out"]
        {{this.Builder.ToString().TrimEnd()}}
        }
        """;

    protected StringBuilder Builder { get; } = new();
    private readonly Dictionary<ParseNode, int> nodeNames = new();
    private readonly Stack<int> parentStack = new();

    protected int GetNodeName(ParseNode parseTree)
    {
        if (!this.nodeNames.TryGetValue(parseTree, out var name))
        {
            name = this.nodeNames.Count;
            this.nodeNames.Add(parseTree, name);
        }
        return name;
    }

    protected bool TryGetParentName(out int parentName) =>
        this.parentStack.TryPeek(out parentName);

    protected virtual NodeAction GetNodeAction(ParseNode tree) => NodeAction.Print;

    protected void PrintTree(ParseNode tree)
    {
        switch (this.GetNodeAction(tree))
        {
        case NodeAction.Print:
            this.PrintSingle(tree);
            this.parentStack.Push(this.GetNodeName(tree));
            foreach (var child in tree.Children) this.PrintTree(child);
            this.parentStack.Pop();
            break;

        case NodeAction.Skip:
            foreach (var child in tree.Children) this.PrintTree(child);
            break;

        case NodeAction.TerminateChildren:
            this.PrintSingle(tree);
            break;

        case NodeAction.Terminate:
            break;

        default:
            throw new InvalidOperationException();
        }
    }

    protected abstract void PrintSingle(ParseNode tree);
}
