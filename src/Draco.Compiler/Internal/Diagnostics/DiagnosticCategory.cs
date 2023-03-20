using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Diagnostics;

/// <summary>
/// Possible categories of diagnostic.
/// </summary>
internal enum DiagnosticCategory
{
    InternalCompiler = 0,
    Syntax = 1,
    SymbolResolution = 2,
    TypeChecking = 3,
    Dataflow = 4,
    Codegen = 5,
}
