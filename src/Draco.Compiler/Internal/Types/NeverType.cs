using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Types;

/// <summary>
/// Represents the bottom-type.
/// </summary>
internal sealed class NeverType : Type
{
    public static NeverType Instance { get; } = new();

    private NeverType()
    {
    }

    public override string ToString() => "<never>";
}
