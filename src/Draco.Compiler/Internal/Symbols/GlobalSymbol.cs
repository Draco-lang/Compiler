using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A global variable.
/// </summary>
internal abstract partial class GlobalSymbol : VariableSymbol
{
    public override void ToDot(DotGraphBuilder<Symbol> builder)
    {
        builder
            .AddVertex(this)
            .WithLabel($"global '{this.Name}'");
    }
}
