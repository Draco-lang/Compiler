using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Internal.Types;

namespace Draco.Compiler.Internal.Symbols.Synthetized;

/// <summary>
/// Not a true symbol, represents function overloads.
/// </summary>
internal sealed class OverloadSymbol : Symbol
{
    /// <summary>
    /// The candidate functions in the overload set.
    /// </summary>
    public ImmutableArray<FunctionSymbol> Functions { get; }

    public override Symbol? ContainingSymbol => throw new System.NotImplementedException();

    public override string Name => this.Functions[0].Name;

    public OverloadSymbol(ImmutableArray<FunctionSymbol> functions)
    {
        if (functions.Length == 0)
        {
            throw new System.ArgumentException("overloads must contain at least one function", nameof(functions));
        }
        this.Functions = functions;
    }

    public override ISymbol ToApiSymbol() => throw new System.NotSupportedException();
}
