using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class SynthetizedUnaryOperatorSymbol : UnaryOperatorSymbol
{
    public override ParameterSymbol Operand => throw new System.NotImplementedException();
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotImplementedException();

    public override Type ReturnType => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public SynthetizedUnaryOperatorSymbol(TokenKind token)
    {
        this.Name = GetUnaryOperatorName(token);
    }
}
