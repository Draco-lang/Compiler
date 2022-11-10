using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;
using Draco.Query;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// Utility for printing the result of symbol resolution in a DOT graph.
/// </summary>
internal sealed class ScopeTreePrinter
{
    public static string Print(QueryDatabase db, ParseTree parseTree)
    {
        var printer = new ScopeTreePrinter(db);
        printer.PrintHeader();
        printer.Print(parseTree);
        printer.PrintFooter();
        return printer.result.ToString();
    }

    private readonly QueryDatabase db;
    private readonly StringBuilder result = new();
    private readonly Dictionary<ParseTree, int> nodeNames = new();

    private ScopeTreePrinter(QueryDatabase db)
    {
        this.db = db;
    }

    private int GetNodeName(ParseTree parseTree)
    {
        if (!this.nodeNames.TryGetValue(parseTree, out var name))
        {
            name = this.nodeNames.Count;
            this.nodeNames.Add(parseTree, name);
        }
        return name;
    }

    private void PrintHeader() => this.result.AppendLine("graph scope_tree {");
    private void PrintFooter() => this.result.AppendLine("}");

    private void Print(ParseTree parseTree)
    {
        var name = this.GetNodeName(parseTree);
        // Node text
        this.result.AppendLine($"  {name} [label=\"{parseTree.GetType().Name}\"]");

        // TODO: Scope data?
        // TODO: Reference data?

        // Parent relation
        if (parseTree.Parent is not null)
        {
            var parentName = this.GetNodeName(parseTree.Parent);
            this.result.AppendLine($"  {name} -- {parentName}");
        }
        // Recurse to children
        foreach (var child in parseTree.Children) this.Print(child);
    }
}
