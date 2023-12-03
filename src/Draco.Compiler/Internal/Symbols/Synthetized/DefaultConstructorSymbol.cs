using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.BoundTree;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A default constructor for in-source types.
/// </summary>
internal sealed class DefaultConstructorSymbol : SynthetizedFunctionSymbol
{
    public override Symbol ContainingSymbol { get; }

    public override string Name => ".ctor";
    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;
    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;
    public override bool IsStatic => false;

    public override BoundStatement Body => throw new NotImplementedException();

    public DefaultConstructorSymbol(Symbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
    }
}
