using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Internal.DracoIr;

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
    public IReadOnlyDictionary<string, IReadOnlyProcedure> Procedures { get; }
}

/// <summary>
/// Interface for a single procedure.
/// </summary>
internal interface IReadOnlyProcedure
{
    /// <summary>
    /// The name of this procedure.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The return-type of this procedure.
    /// </summary>
    public Type ReturnType { get; }

    /// <summary>
    /// The parameters of this procedure.
    /// </summary>
    public IReadOnlyList<Value.Parameter> Parameters { get; }

    /// <summary>
    /// The entry-point of the block.
    /// </summary>
    public IReadOnlyBasicBlock Entry { get; }

    /// <summary>
    /// All <see cref="IReadOnlyBasicBlock"/>s in this procedure in the order they were written.
    /// </summary>
    public IReadOnlyList<IReadOnlyBasicBlock> BasicBlocks { get; }

    /// <summary>
    /// All <see cref="IReadOnlyInstruction"/>s in this procedure in the order of their basic blocks.
    /// </summary>
    public IEnumerable<IReadOnlyInstruction> Instructions { get; }
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
    /// Allocate stack-memory for a local.
    /// </summary>
    Alloc,

    /// <summary>
    /// Store into a local.
    /// </summary>
    Store,

    /// <summary>
    /// Load from a local.
    /// </summary>
    Load,

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

    /// <summary>
    /// Integer subtraction.
    /// </summary>
    SubInt,

    /// <summary>
    /// Integer multiplication.
    /// </summary>
    MulInt,

    /// <summary>
    /// Integer division.
    /// </summary>
    DivInt,

    /// <summary>
    /// Integer remainder.
    /// </summary>
    RemInt,

    /// <summary>
    /// Integer less-than comparison.
    /// </summary>
    LessInt,

    /// <summary>
    /// Integer less-or-equal comparison.
    /// </summary>
    LessEqualInt,

    /// <summary>
    /// Integer equality comparison.
    /// </summary>
    EqualInt,

    /// <summary>
    /// Integer negation.
    /// </summary>
    NegInt,

    /// <summary>
    /// Boolean negation.
    /// </summary>
    NotBool,

    /// <summary>
    /// A procedure call.
    /// </summary>
    Call,
}

/// <summary>
/// Base for all values.
/// </summary>
internal abstract partial record class Value
{
    /// <summary>
    /// The <see cref="DracoIr.Type"/> of this <see cref="Value"/>.
    /// </summary>
    public abstract Type Type { get; }
}

internal abstract partial record class Value
{
    public sealed record class Parameter(Type Type, string Name, int Index) : Value
    {
        public override Type Type { get; } = Type;

        public string ToFullString() => $"{this.Type} {this.Name}";
        public override string ToString() => this.Name;
    }

    /// <summary>
    /// A unitary value.
    /// </summary>
    public sealed record class Unit : Value
    {
        public static Unit Instance { get; } = new();

        public override Type Type => Type.Unit;

        public override string ToString() => "unit";
    }

    /// <summary>
    /// A register value.
    /// </summary>
    /// <param name="Type">The <see cref="DracoIr.Type"/> of this <see cref="Register"/>.</param>
    public sealed record class Register(Type Type) : Value
    {
        private static int idCounter = -1;

        private readonly int id = Interlocked.Increment(ref idCounter);

        public override Type Type { get; } = Type;

        public string ToFullString() => $"{this.Type} {this}";
        public override string ToString() => $"reg_{this.id}";
    }

    /// <summary>
    ///  A constant value.
    /// </summary>
    /// <param name="Value">The constant value.</param>
    public sealed record class Constant(object? Value) : Value
    {
        public override Type Type => this.Value switch
        {
            bool => Type.Bool,
            int => Type.Int32,
            _ => throw new InvalidOperationException(),
        };

        public override string ToString() => this.Value?.ToString() ?? "null";
    }
}

/// <summary>
/// Base for all types.
/// </summary>
internal abstract partial record class Type
{
    /// <summary>
    /// A builtin <see cref="DracoIr.Type"/>.
    /// </summary>
    /// <param name="Type">The native <see cref="System.Type"/> referenced.</param>
    public sealed record class Builtin(System.Type Type) : Type
    {
        public override string ToString() => this.Type.FullName ?? this.Type.Name;
    }

    /// <summary>
    /// A pointer type.
    /// </summary>
    /// <param name="Element">The pointer element.</param>
    public sealed record class Ptr(Type Element) : Type
    {
        public override string ToString() => $"{this.Element}*";
    }

    /// <summary>
    /// A procedure type.
    /// </summary>
    /// <param name="Args">The argument types.</param>
    /// <param name="Ret">The return type.</param>
    public sealed record class Proc(ImmutableArray<Type> Args, Type Ret) : Type
    {
        public override string ToString() => $"proc({string.Join(", ", this.Args)}) -> {this.Ret}";
    }
}

// Builtins
internal abstract partial record class Type
{
    public static Type Unit { get; } = new Builtin(typeof(void));
    public static Type Bool { get; } = new Builtin(typeof(bool));
    public static Type Int32 { get; } = new Builtin(typeof(int));
}
