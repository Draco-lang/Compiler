using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

internal sealed class SynthetizedBinaryOperatorSymbol : BinaryOperatorSymbol
{
    public override ParameterSymbol Left { get; }
    public override ParameterSymbol Right { get; }
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray.Create(this.Left, this.Right);

    public override Type ReturnType { get; }
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public SynthetizedBinaryOperatorSymbol(TokenKind token, Type leftType, Type rightType, Type returnType)
    {
        this.Name = GetBinaryOperatorName(token);
        this.Left = new SynthetizedParameterSymbol(leftType);
        this.Right = new SynthetizedParameterSymbol(rightType);
        this.ReturnType = returnType;
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
