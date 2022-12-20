using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

// Interfaces //////////////////////////////////////////////////////////////////

/// <summary>
/// Interface for a compilation unit.
/// </summary>
internal interface IReadOnlyAssembly
{
    /// <summary>
    /// The name of this assembly.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The procedures defined in this assembly.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyProcecude> Procedures { get; }
}

/// <summary>
/// Interface for a single procedure.
/// </summary>
internal interface IReadOnlyProcecude
{
    /// <summary>
    /// The name of this procedure.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The entry-point of the block.
    /// </summary>
    public IReadOnlyBasicBlock Entry { get; }
}

/// <summary>
/// The interface for a basic block.
/// </summary>
internal interface IReadOnlyBasicBlock
{
    /// <summary>
    /// The instructions in this block.
    /// </summary>
    public IReadOnlyList<IReadOnlyInstruction> Instructions { get; }
}

/// <summary>
/// Interface for a single instruction.
/// </summary>
internal interface IReadOnlyInstruction
{
    /// <summary>
    /// The kind of this instruction.
    /// </summary>
    public InstructionKind Kind { get; }

    /// <summary>
    /// True, if this is a branching instruction.
    /// </summary>
    public bool IsBranch { get; }

    /// <summary>
    /// Retrieves an operand of this instruction.
    /// </summary>
    /// <typeparam name="T">The type of the operand.</typeparam>
    /// <param name="index">The indef of the operand.</param>
    /// <returns>The operand at index <paramref name="index"/>.</returns>
    public T OperandAt<T>(int index);
}

/// <summary>
/// The different kinds of instructions.
/// </summary>
internal enum InstructionKind
{
    /// <summary>
    /// No operation.
    /// </summary>
    Nop,

    /// <summary>
    /// Return from a procedure.
    /// </summary>
    Ret,

    /// <summary>
    /// Unconditional jump.
    /// </summary>
    Jmp,

    /// <summary>
    /// Conditional jump.
    /// </summary>
    JmpIf,

    /// <summary>
    /// Integer addition.
    /// </summary>
    AddInt,
}

// Implementations /////////////////////////////////////////////////////////////

// TODO
