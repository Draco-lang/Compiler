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
    public T GetOperandAt<T>(int index);
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

/// <summary>
/// A base class for instructions.
/// </summary>
internal abstract partial class Instruction : IReadOnlyInstruction
{
    private static readonly InstructionKind[] branchInstructions = new[]
    {
        InstructionKind.Ret,
        InstructionKind.Jmp,
        InstructionKind.JmpIf,
    };

    public InstructionKind Kind { get; set; }
    public bool IsBranch => branchInstructions.Contains(this.Kind);

    public Instruction(InstructionKind kind)
    {
        this.Kind = kind;
    }

    public abstract T GetOperandAt<T>(int index);
    public abstract void SetOperandAt<T>(int index, T value);
}

// Implementations
internal abstract partial class Instruction
{
    private sealed class Instruction0 : Instruction
    {
        public Instruction0(InstructionKind kind)
            : base(kind)
        {
        }

        public override T GetOperandAt<T>(int index) => throw new NotSupportedException("nullary instruction has no operands");
        public override void SetOperandAt<T>(int index, T value) => throw new NotSupportedException("nullary instruction has no operands");
    }

    private sealed class Instruction1<T1> : Instruction
    {
        private T1 operand1;

        public Instruction1(InstructionKind kind, T1 operand1)
            : base(kind)
        {
            this.operand1 = operand1;
        }

        public override T GetOperandAt<T>(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
            return (T)(object)this.operand1!;
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
            this.operand1 = (T1)(object)value!;
        }
    }

    private sealed class Instruction2<T1, T2> : Instruction
    {
        private T1 operand1;
        private T2 operand2;

        public Instruction2(InstructionKind kind, T1 operand1, T2 operand2)
            : base(kind)
        {
            this.operand1 = operand1;
            this.operand2 = operand2;
        }

        public override T GetOperandAt<T>(int index)
        {
            if (index == 0)
            {
                if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
                return (T)(object)this.operand1!;
            }
            if (index == 1)
            {
                if (typeof(T) != typeof(T2)) throw new InvalidOperationException("invalid operand type");
                return (T)(object)this.operand2!;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            if (index == 0)
            {
                if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
                this.operand1 = (T1)(object)value!;
                return;
            }
            if (index == 1)
            {
                if (typeof(T) != typeof(T2)) throw new InvalidOperationException("invalid operand type");
                this.operand2 = (T2)(object)value!;
                return;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    private sealed class Instruction3<T1, T2, T3> : Instruction
    {
        private T1 operand1;
        private T2 operand2;
        private T3 operand3;

        public Instruction3(InstructionKind kind, T1 operand1, T2 operand2, T3 operand3)
            : base(kind)
        {
            this.operand1 = operand1;
            this.operand2 = operand2;
            this.operand3 = operand3;
        }

        public override T GetOperandAt<T>(int index)
        {
            if (index == 0)
            {
                if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
                return (T)(object)this.operand1!;
            }
            if (index == 1)
            {
                if (typeof(T) != typeof(T2)) throw new InvalidOperationException("invalid operand type");
                return (T)(object)this.operand2!;
            }
            if (index == 2)
            {
                if (typeof(T) != typeof(T3)) throw new InvalidOperationException("invalid operand type");
                return (T)(object)this.operand3!;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            if (index == 0)
            {
                if (typeof(T) != typeof(T1)) throw new InvalidOperationException("invalid operand type");
                this.operand1 = (T1)(object)value!;
                return;
            }
            if (index == 1)
            {
                if (typeof(T) != typeof(T2)) throw new InvalidOperationException("invalid operand type");
                this.operand2 = (T2)(object)value!;
                return;
            }
            if (index == 2)
            {
                if (typeof(T) != typeof(T3)) throw new InvalidOperationException("invalid operand type");
                this.operand3 = (T3)(object)value!;
                return;
            }
            throw new ArgumentOutOfRangeException(nameof(index));
        }
    }
}
