using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Diagnostics;

namespace Draco.Compiler.Internal.Semantics;

/// <summary>
/// The base for all types in the compiler.
/// </summary>
internal abstract partial record class Type
{
    /// <summary>
    /// True, if this is an error type.
    /// </summary>
    public virtual bool IsError => false;

    /// <summary>
    /// All diagnostics related to this type.
    /// </summary>
    public virtual ImmutableArray<Diagnostic> Diagnostics => ImmutableArray<Diagnostic>.Empty;
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
