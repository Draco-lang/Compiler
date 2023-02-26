using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A parameter constructed by the compiler.
/// </summary>
internal sealed class SynthetizedParameterSymbol : ParameterSymbol
{
    public override Type Type => throw new System.NotImplementedException();

    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();
}
