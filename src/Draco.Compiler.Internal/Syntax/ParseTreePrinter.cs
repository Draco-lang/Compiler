using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Utilities;
using static Draco.Compiler.Internal.Syntax.ParseTree;

namespace Draco.Compiler.Internal.Syntax;

/// <summary>
/// Prints the <see cref="ParseTree"/> as the text it was parsed from.
/// </summary>
internal sealed class CodeParseTreePrinter : ParseTreeVisitorBase<Unit>
{
    /// <summary>
    /// Prints the <see cref="ParseTree"/> as the text it was parsed from.
    /// </summary>
    /// <param name="tree">The tree to print.</param>
    /// <returns>The <paramref name="tree"/> printed to text, identical to the text it was parsed from.</returns>
    public static string Print(ParseTree tree)
    {
        var printer = new CodeParseTreePrinter();
        printer.Visit(tree);
        return printer.code.ToString();
    }

    private readonly StringBuilder code = new();

    private CodeParseTreePrinter()
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

/// <summary>
/// Prints the parse-tree in a debuggable form with type names and explicit hierarchy.
/// </summary>
internal sealed class DebugParseTreePrinter
{
    /// <summary>
    /// Prints the parse-tree in a debuggable form with type names and explicit hierarchy.
    /// </summary>
    /// <param name="tree">The tree to print.</param>
    /// <returns>The <paramref name="tree"/> printed in a debuggable form.</returns>
    public static string Print(ParseTree tree)
    {
        var printer = new DebugParseTreePrinter();
        printer.AppendParseTree(tree);
        return printer.code.ToString();
    }

    private readonly StringBuilder code = new();
    private int indentation;

    private DebugParseTreePrinter()
    {
    }

    private DebugParseTreePrinter AppendObject(object? obj) => obj switch
    {
        Token t => this.AppendToken(t),
        ParseTree t => this.AppendParseTree(t),
        string s => this.Append(s),
        IEnumerable e => this.AppendList(e),
        object o when o.GetType() is var type
                   && type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(Enclosed<>) =>
            this.AppendSubtree("Enclosed", o),
        object o when o.GetType() is var type
                   && type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(PunctuatedList<>) =>
            this.AppendList((IEnumerable)type.GetProperty(nameof(PunctuatedList<int>.Elements))!.GetValue(o)!),
        object o when o.GetType() is var type
                   && type.IsGenericType
                   && type.GetGenericTypeDefinition() == typeof(Punctuated<>) =>
            this.AppendSubtree("Punctuated", o),
        object o => this.Append(o.ToString() ?? string.Empty),
        null => this.Append("null"),
    };

    private DebugParseTreePrinter AppendList(IEnumerable e) => this.AppendIndented(
        e.Cast<object?>().Select(o => new KeyValuePair<string?, object?>(null, o)),
        open: '[',
        close: ']');

    private DebugParseTreePrinter AppendParseTree(ParseTree t) => this.AppendSubtree(t.GetType().Name, t);

    private DebugParseTreePrinter AppendToken(Token t)
    {
        this.Append("'").Append(t.Text).Append("'");
        var valueText = t.ValueText;
        if (valueText is not null && valueText != t.Text) this.Append($" (value={valueText})");
        if (t.Diagnostics.Length > 0)
        {
            this.Append(" [");
            this.Append(string.Join(", ", t.Diagnostics.Select(DiagnosticToString)));
            this.Append("]");
        }
        return this;
    }

    private DebugParseTreePrinter AppendSubtree(string name, object tree) => this
        .Append(name)
        .Append(" ")
        .AppendIndented(tree
            .GetType()
            .GetProperties()
            .Where(p => tree is not ParseTree || p.Name != "Children")
            .Select(p => new KeyValuePair<string?, object?>(p.Name, p.GetValue(tree))),
            open: '{',
            close: '}');

    private DebugParseTreePrinter AppendIndented(IEnumerable<KeyValuePair<string?, object?>> values, char open, char close)
    {
        if (!values.Any()) return this.Append($"{open}{close}");

        this.AppendLine(open.ToString());
        ++this.indentation;
        foreach (var (key, value) in values)
        {
            this.AppendIndentation();
            if (key is not null) this.Append(key).Append(": ");
            this.AppendObject(value).AppendLine(", ");
        }
        --this.indentation;
        this.AppendIndentation();
        return this.Append(close.ToString());
    }

    private DebugParseTreePrinter Append(string text)
    {
        this.code.Append(text);
        return this;
    }

    private DebugParseTreePrinter AppendLine(string text = "")
    {
        this.code.AppendLine(text);
        return this;
    }

    private DebugParseTreePrinter AppendIndentation()
    {
        for (var i = 0; i < this.indentation; ++i) this.code.Append("  ");
        return this;
    }

    private static string DiagnosticToString(Diagnostic diagnostic) =>
           string.Format(diagnostic.Format, diagnostic.FormatArgs);
}
