using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// A mutable implementation of <see cref="IBasicBlock"/>.
/// </summary>
internal sealed class BasicBlock : IBasicBlock
{
    public IInstruction FirstInstruction => throw new NotImplementedException();
    public IInstruction LastInstruction => throw new NotImplementedException();
    public IEnumerable<IInstruction> Instructions => throw new NotImplementedException();
}
