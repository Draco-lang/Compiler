using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        graph scope_tree {
          rankdir="BT"
          graph[ordering="out"]
        {{this.Builder.ToString().TrimEnd()}}
        }
        """;

    protected StringBuilder Builder { get; } = new();
    private readonly Dictionary<ParseTree, int> nodeNames = new();
    private readonly Stack<int> parentStack = new();

    protected int GetNodeName(ParseTree parseTree)
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

    protected virtual NodeAction GetNodeAction(ParseTree tree) => NodeAction.Print;

    protected void Print(ParseTree tree)
    {
        switch (this.GetNodeAction(tree))
        {
        case NodeAction.Print:
            this.PrintSingle(tree);
            this.parentStack.Push(this.GetNodeName(tree));
            foreach (var child in tree.Children) this.Print(child);
            this.parentStack.Pop();
            break;

        case NodeAction.Skip:
            foreach (var child in tree.Children) this.Print(child);
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

    protected abstract void PrintSingle(ParseTree tree);
}
