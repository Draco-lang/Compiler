using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : Type
{
    /// <summary>
    /// The substitution of this type variable.
    /// </summary>
    public Type Substitution
    {
        get
        {
            if (this.substitution is null) return this;
            // Pruning
            if (this.substitution is TypeVariable var) this.substitution = var.Substitution;
            return this.substitution;
        }
        set
        {
            if (this.substitution is not null) throw new InvalidOperationException("tried to substitute type variable multiple times");
            this.substitution = value;
        }
    }
    private Type? substitution;
}
