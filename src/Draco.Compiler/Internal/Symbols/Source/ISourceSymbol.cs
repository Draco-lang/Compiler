using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Symbols.Source;

/// <summary>
/// An interface for all symbols defined in-source.
/// </summary>
internal interface ISourceSymbol
{
    /// <summary>
    /// Enforced binding of the symbol. It does not recurse to bind members of the symbol.
    /// </summary>
    /// <param name="diagnostics">The bag to report diagnostics to.</param>
    public void Bind(DiagnosticBag diagnostics);
}
