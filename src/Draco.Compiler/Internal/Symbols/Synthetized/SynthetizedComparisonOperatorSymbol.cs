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
    public override ParameterSymbol Left => throw new System.NotImplementedException();
    public override ParameterSymbol Right => throw new System.NotImplementedException();
    public override ImmutableArray<ParameterSymbol> Parameters => throw new System.NotImplementedException();

    public override Type ReturnType => throw new System.NotImplementedException();
    public override Symbol? ContainingSymbol => null;

    public override string Name { get; }

    public SynthetizedComparisonOperatorSymbol(TokenKind token)
    {
        this.Name = GetComparisonOperatorName(token);
    }

    public override ISymbol ToApiSymbol() => throw new System.NotImplementedException();
}
