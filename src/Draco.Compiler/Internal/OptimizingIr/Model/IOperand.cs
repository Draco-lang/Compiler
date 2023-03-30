using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.OptimizingIr.Model;

/// <summary>
/// The interface of all operands that an instruction can hold.
/// </summary>
internal interface IOperand
{
    /// <summary>
    /// Returns a string representation of the operand.
    ///
    /// The reason that <see cref="object.ToString"/> is not used is because some entities are printed
    /// with <see cref="object.ToString"/>, when they are not operands, for example procedures and basic blocks.
    /// </summary>
    /// <returns>The string representation of this operand.</returns>
    public string ToOperandString();
}
