using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Syntax.Formatting;

namespace Draco.Formatter.Csharp;
public class CSharpFormatterSettings : FormatterSettings
{
    public static new CSharpFormatterSettings Default { get; } = new();
    public static CSharpFormatterSettings DracoStyle { get; } = new()
    {
        NewLineBeforeConstructorInitializer = true,
        LineWidth = 120
    };

    public bool NewLineBeforeConstructorInitializer { get; init; }
}
