using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Binds the untyped tree to a fully typed bound tree.
/// </summary>
internal abstract partial class TypedBinder
{
    /// <summary>
    /// Binds the given module to be fully typed.
    /// </summary>
    /// <param name="moduleSymbol">The module to bind.</param>
    /// <returns>The fully bound and typed module.</returns>
    public ModuleSymbol Bind(ModuleSymbol moduleSymbol)
    {
        throw new NotImplementedException();
    }
}
