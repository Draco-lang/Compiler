using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Builds an <see cref="Assembly"/>.
/// </summary>
internal sealed class AssemblyBuilder
{
}

/// <summary>
/// Builds a <see cref="Proc"/>.
/// </summary>
internal sealed class ProcBuilder
{
    /// <summary>
    /// The name of the procedure.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The parameters for this procedure.
    /// </summary>
    public ImmutableArray<Value.Param>.Builder Params { get; set; } = ImmutableArray.CreateBuilder<Value.Param>();

    /// <summary>
    /// The return type of the procedure.
    /// </summary>
    public Type ReturnType { get; set; } = Type.Void;
}
