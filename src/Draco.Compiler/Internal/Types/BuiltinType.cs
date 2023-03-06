using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents a builtin type.
/// </summary>
internal sealed class BuiltinType : Type
{
    /// <summary>
    /// The underlying system type.
    /// </summary>
    public System.Type UnderylingType { get; }

    public BuiltinType(System.Type underylingType)
    {
        this.UnderylingType = underylingType;
    }
}
