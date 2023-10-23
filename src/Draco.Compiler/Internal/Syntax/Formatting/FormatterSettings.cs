using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Syntax.Formatting;

/// <summary>
/// The settings of the formatter.
/// </summary>
internal sealed class FormatterSettings
{
    /// <summary>
    /// The default formatting settings.
    /// </summary>
    public static FormatterSettings Default { get; } = new();

    /// <summary>
    /// The newline sequence.
    /// </summary>
    public string Newline { get; } = "\n";
}
