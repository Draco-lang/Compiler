using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a type definition.
/// </summary>
internal abstract partial class TypeSymbol : Symbol
{
    /// <summary>
    /// The defined type.
    /// </summary>
    public abstract Type Type { get; }
}
