using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A comparison operator constructed by the compiler.
/// </summary>
internal sealed class SynthetizedComparisonOperatorSymbol : ComparisonOperatorSymbol
{
    public override ParameterSymbol Left => throw new System.NotImplementedException();
    public override ParameterSymbol Right => throw new System.NotImplementedException();
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotImplementedException();

    public override Type ReturnType => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol => null;
}
