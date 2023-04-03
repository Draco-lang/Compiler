using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// A pseudo-instruction for representing the start of a local scope.
/// </summary>
internal sealed class StartScope : InstructionBase
{
    /// <summary>
    /// The locals introduced in this scope.
    /// </summary>
    public ImmutableArray<LocalSymbol> Locals { get; }

    public override bool IsValidInUnreachableContext => true;

    public StartScope(ImmutableArray<LocalSymbol> locals)
    {
        this.Locals = locals;
    }

    public override StartScope Clone() => new(this.Locals);

    public override string ToString()
    {
        var result = new StringBuilder();
        result.Append("@scope start [");
        result.AppendJoin(", ", this.Locals.Select(x => x.Name));
        result.Append(']');
        return result.ToString();
    }
}
