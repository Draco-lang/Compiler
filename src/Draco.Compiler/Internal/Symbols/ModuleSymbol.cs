using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// Represents a compilation unit.
/// </summary>
internal abstract partial class ModuleSymbol : Symbol
{
    /// <summary>
    /// All members within this module.
    /// </summary>
    public abstract ImmutableArray<Symbol> Members { get; }

    public override void ToDot(DotGraphBuilder<Symbol> builder)
    {
        builder
            .AddVertex(this)
            .WithLabel($"module '{this.Name}'");
        foreach (var m in this.Members)
        {
            builder.AddEdge(this, m);
            m.ToDot(builder);
        }
    }
}
