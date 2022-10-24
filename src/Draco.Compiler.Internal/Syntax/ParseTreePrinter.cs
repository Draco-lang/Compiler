using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Syntax;

internal sealed class ParseTreePrinter : ParseTreeVisitorBase<Unit>
{
    public static string Print(ParseTree tree)
    {
        var printer = new ParseTreePrinter();
        printer.Visit(tree);
        return printer.code.ToString();
    }

    private readonly StringBuilder code = new();

    private ParseTreePrinter()
    {
    }

    public override Unit VisitToken(Token token)
    {
        foreach (var t in token.LeadingTrivia) this.code.Append(t.Text);
        this.code.Append(token.Text);
        foreach (var t in token.TrailingTrivia) this.code.Append(t.Text);
        return default;
    }
}
