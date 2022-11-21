using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base for all types in the compiler.
/// </summary>
internal abstract partial record class Type
{
}

internal abstract partial record class Type
{
    /// <summary>
    /// Represents a native, builtin type.
    /// </summary>
    public sealed record class Builtin(System.Type Type) : Type
    {
        public override string ToString() => this.Type.Name;
    }
}
