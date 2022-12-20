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
    /// The number of operands.
    /// </summary>
    public int OperandCount { get; }

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
/// An <see cref="IReadOnlyAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IReadOnlyAssembly
{
    public string Name { get; set; } = "assembly";

    public IDictionary<string, Procedure> Procedures { get; } = new Dictionary<string, Procedure>();
    // TODO: We need a projection wrapper type
    IReadOnlyDictionary<string, IReadOnlyProcecude> IReadOnlyAssembly.Procedures => throw new NotImplementedException();

    public Assembly(string name)
    {
        this.Name = name;
    }
}

/// <summary>
/// An <see cref="IReadOnlyProcecude"/> implementation.
/// </summary>
internal sealed class Procedure : IReadOnlyProcecude
{
    public string Name { get; }

    public BasicBlock Entry { get; set; } = new();
    IReadOnlyBasicBlock IReadOnlyProcecude.Entry => this.Entry;

    public Procedure(string name)
    {
        this.Name = name;
    }
}

internal sealed class BasicBlock : IReadOnlyBasicBlock
{
    public IList<Instruction> Instructions { get; } = new List<Instruction>();
    // TODO: We need a projection wrapper type
    IReadOnlyList<IReadOnlyInstruction> IReadOnlyBasicBlock.Instructions => throw new NotImplementedException();
}

/// <summary>
/// Base for all values.
/// </summary>
internal abstract record class Value
{
    /// <summary>
    /// A register value.
    /// </summary>
    public sealed record class Register : Value;
}

/// <summary>
/// Base for all types.
/// </summary>
internal abstract record class Type
{
}

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

    public InstructionKind Kind { get; }
    public bool IsBranch => branchInstructions.Contains(this.Kind);
    public abstract int OperandCount { get; }

    public Instruction(InstructionKind kind)
    {
        this.Kind = kind;
    }

    public abstract T GetOperandAt<T>(int index);
    public abstract void SetOperandAt<T>(int index, T value);
}

// Factory
internal abstract partial class Instruction
{
    public static Instruction Nop() =>
        new Instruction0(InstructionKind.Nop);
    public static Instruction Ret(Value value) =>
        new Instruction1<Value>(InstructionKind.Ret, value);
    public static Instruction Jmp(IReadOnlyBasicBlock bb) =>
        new Instruction1<IReadOnlyBasicBlock>(InstructionKind.Jmp, bb);
    public static Instruction JmpIf(Value condition, IReadOnlyBasicBlock then, IReadOnlyBasicBlock els) =>
        new Instruction3<Value, IReadOnlyBasicBlock, IReadOnlyBasicBlock>(InstructionKind.JmpIf, condition, then, els);
    public static Instruction AddInt(Value.Register target, Value a, Value b) =>
        new Instruction3<Value.Register, Value, Value>(InstructionKind.AddInt, target, a, b);
}

// Implementations
internal abstract partial class Instruction
{
    private sealed class Instruction0 : Instruction
    {
        public override int OperandCount => 0;

        public Instruction0(InstructionKind kind)
            : base(kind)
        {
        }

        public override T GetOperandAt<T>(int index) => throw new NotSupportedException("nullary instruction has no operands");
        public override void SetOperandAt<T>(int index, T value) => throw new NotSupportedException("nullary instruction has no operands");
    }

    private sealed class Instruction1<T1> : Instruction
    {
        public override int OperandCount => 1;

        private T1 operand1;

        public Instruction1(InstructionKind kind, T1 operand1)
            : base(kind)
        {
            this.operand1 = operand1;
        }

        public override T GetOperandAt<T>(int index)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (this.operand1 is not T op1) throw new InvalidOperationException("invalid operand type");
            return op1;
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            if (index != 0) throw new ArgumentOutOfRangeException(nameof(index));
            if (this.operand1 is not T) throw new InvalidOperationException("invalid operand type");
            this.operand1 = (T1)(object)value!;
        }
    }

    private sealed class Instruction2<T1, T2> : Instruction
    {
        public override int OperandCount => 2;

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
            switch (index)
            {
            case 0:
                if (this.operand1 is not T op1) throw new InvalidOperationException("invalid operand type");
                return op1;
            case 1:
                if (this.operand2 is not T op2) throw new InvalidOperationException("invalid operand type");
                return op2;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            switch (index)
            {
            case 0:
                if (this.operand1 is not T) throw new InvalidOperationException("invalid operand type");
                this.operand1 = (T1)(object)value!;
                break;
            case 1:
                if (this.operand2 is not T) throw new InvalidOperationException("invalid operand type");
                this.operand2 = (T2)(object)value!;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }

    private sealed class Instruction3<T1, T2, T3> : Instruction
    {
        public override int OperandCount => 3;

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
            switch (index)
            {
            case 0:
                if (this.operand1 is not T op1) throw new InvalidOperationException("invalid operand type");
                return op1;
            case 1:
                if (this.operand2 is not T op2) throw new InvalidOperationException("invalid operand type");
                return op2;
            case 2:
                if (this.operand3 is not T op3) throw new InvalidOperationException("invalid operand type");
                return op3;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        public override void SetOperandAt<T>(int index, T value)
        {
            switch (index)
            {
            case 0:
                if (this.operand1 is not T) throw new InvalidOperationException("invalid operand type");
                this.operand1 = (T1)(object)value!;
                break;
            case 1:
                if (this.operand2 is not T) throw new InvalidOperationException("invalid operand type");
                this.operand2 = (T2)(object)value!;
                break;
            case 2:
                if (this.operand3 is not T) throw new InvalidOperationException("invalid operand type");
                this.operand3 = (T3)(object)value!;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
