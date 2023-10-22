using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// A formatter for the syntax tree.
/// </summary>
internal sealed class SyntaxFormatter
{
    /// <summary>
    /// The settings of the formatter.
    /// </summary>
    public SyntaxFormatterSettings Settings { get; }

    public SyntaxFormatter(SyntaxFormatterSettings settings)
    {
        this.Settings = settings;
    }
}
