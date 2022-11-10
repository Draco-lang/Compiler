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
        printer.PrintTree(parseTree);
        return printer.Code;
    }

    private readonly QueryDatabase db;

    private ScopeTreePrinter(QueryDatabase db)
    {
        this.db = db;
    }

    protected override NodeAction GetNodeAction(ParseTree tree) => tree switch
    {
        _ when this.GetDefinedScope(tree) is not null
            || this.GetDefinedSymbol(tree) is not null
            || this.GetReferencedSymbol(tree) is not null => NodeAction.Print,
        _ => NodeAction.Skip,
    };

    protected override void PrintSingle(ParseTree tree)
    {
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

    private static string InferNodeText(ParseTree tree) => tree switch
    {
        ParseTree.Expr.Name name => name.Identifier.Text,
        ParseTree.TypeExpr.Name name => name.Identifier.Text,
        _ => tree.GetType().Name,
    };

    // TODO: Can we get rid of this pattern?
    private Scope? GetDefinedScope(ParseTree tree)
    {
        async Task<Scope?> Impl() => await SymbolResolution.GetDefinedScope(this.db, tree);
        return Impl().Result;
    }

    // TODO: Can we get rid of this pattern?
    private Symbol? GetDefinedSymbol(ParseTree tree)
    {
        async Task<Symbol?> Impl() => await SymbolResolution.GetDefinedSymbol(this.db, tree);
        return Impl().Result;
    }

    // TODO: Can we get rid of this pattern?
    private Symbol? GetReferencedSymbol(ParseTree tree)
    {
        async Task<Symbol?> Impl() => await SymbolResolution.GetReferencedSymbol(this.db, tree);
        return Impl().Result;
    }
}
