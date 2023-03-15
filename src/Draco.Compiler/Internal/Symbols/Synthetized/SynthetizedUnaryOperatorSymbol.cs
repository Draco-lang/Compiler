using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class SynthetizedUnaryOperatorSymbol : UnaryOperatorSymbol
{
    public override ParameterSymbol Operand { get; }
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray.Create(this.Operand);

    public override Type ReturnType { get; }
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public SynthetizedUnaryOperatorSymbol(TokenKind token, Type operandType, Type returnType)
    {
        this.Name = GetUnaryOperatorName(token);
        this.Operand = new SynthetizedParameterSymbol(operandType);
        this.ReturnType = returnType;
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
