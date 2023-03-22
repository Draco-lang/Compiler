using System.Collections.Immutable;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A comparison operator constructed by the compiler.
/// </summary>
internal sealed class SynthetizedComparisonOperatorSymbol : ComparisonOperatorSymbol
{
    public override ParameterSymbol Left { get; }
    public override ParameterSymbol Right { get; }
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray.Create(this.Left, this.Right);

    public override Type ReturnType => Types.Intrinsics.Bool;
    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name { get; }

    public SynthetizedComparisonOperatorSymbol(TokenKind token, Type leftType, Type rightType)
    {
        this.Name = GetComparisonOperatorName(token);
        this.Left = new SynthetizedParameterSymbol(leftType);
        this.Right = new SynthetizedParameterSymbol(rightType);
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
