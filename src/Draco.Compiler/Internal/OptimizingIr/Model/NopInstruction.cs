using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// Represents no operation.
/// </summary>
internal sealed class NopInstruction : InstructionBase
{
    public override NopInstruction Clone() => new();
}
