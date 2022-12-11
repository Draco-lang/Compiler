using System.Linq;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Query;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Semantics.Symbols;

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

        // Query relevant data
        var scope = this.GetDefinedScope(tree);
        var definedSymbol = this.GetDefinedSymbol(tree);
        var referencedSymbol = this.GetReferencedSymbol(tree);

        // Node text
        var textBuilder = new StringBuilder();
        textBuilder.Append(InferNodeText(tree));
        if (scope is not null)
        {
            // Append scope
            textBuilder
                .Append(@"\n")
                .Append(scope.Kind.ToString())
                .Append(" { ")
                .AppendJoin(", ", scope.Timelines
                    .SelectMany(t => t.Value.Declarations)
                    .Select(d => d.Name))
                .Append(" }");
        }
        if (definedSymbol is not null)
        {
            // Append defined symbol
            textBuilder
                .Append(@"\n")
                .Append("define ")
                .Append(definedSymbol.Name);
        }
        if (referencedSymbol is not null && referencedSymbol.Definition is not null)
        {
            // Append reference info
            var referencedName = this.GetNodeName(referencedSymbol.Definition);
            this.Builder.AppendLine($"  {name} -> {referencedName}");
        }

        this.Builder.AppendLine($"  {name} [label=\"{textBuilder}\"]");

        // Parent relation
        if (this.TryGetParentName(out var parentName))
        {
            this.Builder.AppendLine($"  {name} -> {parentName} [dir=none]");
        }
    }

    private static string InferNodeText(ParseTree tree) => tree switch
    {
        ParseTree.Expr.Name name => name.Identifier.Text,
        ParseTree.TypeExpr.Name name => name.Identifier.Text,
        _ => tree.GetType().Name,
    };

    private IScope? GetDefinedScope(ParseTree tree) =>
        SymbolResolution.GetDefinedScopeOrNull(this.db, tree);

    private ISymbol? GetDefinedSymbol(ParseTree tree) =>
        SymbolResolution.GetDefinedSymbolOrNull(this.db, tree);

    private ISymbol? GetReferencedSymbol(ParseTree tree) =>
        SymbolResolution.GetReferencedSymbolOrNull(this.db, tree);
}
