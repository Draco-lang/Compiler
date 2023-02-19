using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a parameter in a function.
/// </summary>
internal abstract partial class ParameterSymbol : LocalSymbol
{
    public override bool IsMutable => false;
}
