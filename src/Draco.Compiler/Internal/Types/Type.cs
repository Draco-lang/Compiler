using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// The base for all types within the language.
/// </summary>
internal abstract partial class Type
{
    /// <summary>
    /// True, if this is a type variable, false otherwise.
    /// </summary>
    public virtual bool IsTypeVariable => false;

    public override bool Equals(object? obj) => throw new InvalidOperationException("do not use equality for types");
    public override int GetHashCode() => throw new InvalidOperationException("do not use equality for types");

    public abstract override string ToString();
}
