using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// A default constructor for in-source types.
/// </summary>
internal sealed class DefaultConstructorSymbol : IrFunctionSymbol
{
    public override Symbol ContainingSymbol { get; }

    public override CodegenDelegate Codegen => (codegen, target, args) =>
    {
        // TODO
        throw new NotImplementedException();
    };

    public override ImmutableArray<ParameterSymbol> Parameters => ImmutableArray<ParameterSymbol>.Empty;
    public override TypeSymbol ReturnType => IntrinsicSymbols.Unit;
    public override bool IsStatic => false;

    public DefaultConstructorSymbol(Symbol containingSymbol)
    {
        this.ContainingSymbol = containingSymbol;
    }
}
