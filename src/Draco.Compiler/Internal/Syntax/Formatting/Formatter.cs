using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

internal sealed class Formatter : Api.Syntax.SyntaxVisitor
{
    /// <summary>
    /// Formats the given syntax tree.
    /// </summary>
    /// <param name="tree">The syntax tree to format.</param>
    /// <param name="settings">The formatter settings to use.</param>
    /// <returns>The formatted tree.</returns>
    public static string Format(SyntaxTree tree, FormatterSettings? settings = null)
    {
        settings ??= FormatterSettings.Default;

        var formatter = new Formatter(settings, tree);
        var dracoToFormattingTreeVisitor = new DracoToFormattingTreeVisitor();
        var res = tree.Root.Accept(dracoToFormattingTreeVisitor);
        var resStr = res.ToString();
        tree.Root.Accept(formatter);
        return null;
    }

    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public FormatterSettings Settings { get; }

    private Formatter(FormatterSettings settings, SyntaxTree tree)
    {
        this.Settings = settings;
    }
}


