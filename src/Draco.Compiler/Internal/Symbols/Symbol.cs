using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Base for all symbols within the language.
/// </summary>
internal abstract partial class Symbol
{
    /// <summary>
    /// The symbol directly containing this one.
    /// </summary>
    public abstract Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The name of this symbol.
    /// </summary>
    public virtual string Name => string.Empty;

    /// <summary>
    /// Converts the symbol-tree to a DOT graph for debugging purposes.
    /// </summary>
    /// <returns>The DOT graph of the symbol-tree.</returns>
    public string ToDot()
    {
        var builder = new DotGraphBuilder<Symbol>(isDirected: true);
        this.ToDot(builder);
        return builder.ToDot();
    }

    /// <summary>
    /// Turns the subtree of the symbol into a DOT graph.
    /// </summary>
    /// <param name="builder">The builder to use for adding children.</param>
    public virtual void ToDot(DotGraphBuilder<Symbol> builder) { }
}
