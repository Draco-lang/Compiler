using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// Represents a pass that is applicable to an entire assembly.
/// </summary>
internal interface IInterproceduralPass
{
    /// <summary>
    /// Checks, if the given <see cref="Assembly"/> matches the rule that will be applied.
    /// </summary>
    /// <param name="assembly">The assembly to check.</param>
    /// <returns>True, if the pass is applicable to <paramref name="assembly"/>.</returns>
    public bool Matches(IReadOnlyAssembly assembly);

    /// <summary>
    /// Applies the pass to the given <see cref="Assembly"/>.
    /// </summary>
    /// <param name="assembly">The <see cref="Assembly"/> to apply the pass to.</param>
    public void Pass(Assembly assembly);
}

/// <summary>
/// Represents a pass that is applicable to a procedure.
/// </summary>
internal interface IGlobalPass
{
    /// <summary>
    /// Checks, if the given <see cref="Procedure"/> matches the rule that will be applied.
    /// </summary>
    /// <param name="procedure">The procedure to check.</param>
    /// <returns>True, if the pass is applicable to <paramref name="procedure"/>.</returns>
    public bool Matches(IReadOnlyProcecude procedure);

    /// <summary>
    /// Applies the pass to the given <see cref="Procedure"/>.
    /// </summary>
    /// <param name="procedure">The <see cref="Procedure"/> to apply the pass to.</param>
    public void Pass(Procedure procedure);
}

/// <summary>
/// Represents a pass that is applicable to a basic-block.
/// </summary>
internal interface ILocalPass
{
    /// <summary>
    /// Checks, if the given <see cref="BasicBlock"/> matches the rule that will be applied.
    /// </summary>
    /// <param name="basicBlock">The basic block to check.</param>
    /// <returns>True, if the pass is applicable to <paramref name="basicBlock"/>.</returns>
    public bool Matches(IReadOnlyBasicBlock basicBlock);

    /// <summary>
    /// Applies the pass to the given <see cref="BasicBlock"/>.
    /// </summary>
    /// <param name="procedure">The <see cref="BasicBlock"/> to apply the pass to.</param>
    public void Pass(BasicBlock basicBlock);
}

/// <summary>
/// Represents a pass that is applicable to an individual instruction.
/// </summary>
internal interface IInstructionPass
{
    /// <summary>
    /// Checks, if the given <see cref="Instruction"/> matches the rule that will be applied.
    /// </summary>
    /// <param name="instruction">The instruction to check.</param>
    /// <returns>True, if the pass is applicable to <paramref name="instruction"/>.</returns>
    public bool Matches(IReadOnlyInstruction instruction);

    /// <summary>
    /// Applies the pass to the given <see cref="Instruction"/>.
    /// </summary>
    /// <param name="instruction">The <see cref="Instruction"/> to apply the pass to.</param>
    /// <returns>The new <see cref="Instruction"/>, in case it got replaced.</returns>
    public Instruction Pass(Instruction instruction);
}
