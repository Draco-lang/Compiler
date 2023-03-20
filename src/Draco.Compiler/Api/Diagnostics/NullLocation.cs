using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Api.Diagnostics;

/// <summary>
/// Represents no location.
/// </summary>
internal sealed class NullLocation : Location
{
    public override bool IsNone => true;

    public override string ToString() => "<no location>";
}
