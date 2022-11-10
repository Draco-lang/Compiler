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
internal sealed class ScopeTreePrinter : DotGraphParseTreePrinterBase
{
    public static string Print(QueryDatabase db, ParseTree parseTree)
    {
        var printer = new ScopeTreePrinter(db);
        printer.Print(parseTree);
        return printer.Code;
    }

    private readonly QueryDatabase db;

    private ScopeTreePrinter(QueryDatabase db)
    {
        this.db = db;
    }

    protected override NodeAction GetNodeAction(ParseTree tree) => tree switch
    {
        ParseTree.Token => NodeAction.Terminate,
        _ => NodeAction.Print,
    };

    protected override void PrintSingle(ParseTree tree)
    {
        async Task Impl()
        {
            var definedScope = await SymbolResolution.GetDefinedScope(this.db, tree);
            var referencedSymbol = await SymbolResolution.GetReferencedSymbol(this.db, tree);

            var name = this.GetNodeName(tree);

            // Node text
            var text = InferNodeText(tree);
            this.Builder.AppendLine($"  {name} [label=\"{text}\"]");

            // TODO: Scope data?
            // TODO: Reference data?

            // Parent relation
            if (this.TryGetParentName(out var parentName))
            {
                this.Builder.AppendLine($"  {name} -- {parentName}");
            }
        }

        // NOTE: Yes, synchronous wait
        Impl().Wait();
    }

    private static string InferNodeText(ParseTree tree) => tree switch
    {
        ParseTree.Expr.Name name => name.Identifier.Text,
        ParseTree.TypeExpr.Name name => name.Identifier.Text,
        _ => tree.GetType().Name,
    };
}
