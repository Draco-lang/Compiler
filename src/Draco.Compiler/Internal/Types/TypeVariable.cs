using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents an uninferred type that can be substituted.
/// </summary>
internal sealed class TypeVariable : Type
{
    private static int idCounter = -1;

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

    // NOTE: This makes printing types nondeterministic
    private readonly int id = Interlocked.Increment(ref idCounter);

    public override string ToString() => this.substitution is null
        ? $"{StringUtils.IndexToExcelColumnName(this.id)}'"
        : this.substitution.ToString();
}
