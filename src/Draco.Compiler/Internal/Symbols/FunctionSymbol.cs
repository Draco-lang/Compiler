using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a free-function.
/// </summary>
internal abstract partial class FunctionSymbol : Symbol, ITypedSymbol
{
    /// <summary>
    /// The parameters of this function.
    /// </summary>
    public abstract ImmutableArray<ParameterSymbol> Parameters { get; }

    /// <summary>
    /// The return type of this function.
    /// </summary>
    public abstract Type ReturnType { get; }

    public override IEnumerable<Symbol> Members => this.Parameters;

    public Type Type => this.type ??= this.BuildType();
    private Type? type;

    private Type BuildType() => new FunctionType(
        this.Parameters.Select(p => p.Type).ToImmutableArray(),
        this.ReturnType);
}
