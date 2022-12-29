using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
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
    /// The entry point of the assembly, if any.
    /// </summary>
    public IReadOnlyProcedure? EntryPoint { get; }

    /// <summary>
    /// The procedures defined in this assembly.
    /// </summary>
    public IReadOnlyList<IReadOnlyProcedure> Procedures { get; }
}

/// <summary>
/// Interface for a single procedure.
/// </summary>
internal interface IReadOnlyProcedure : IInstructionOperand
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
internal interface IReadOnlyBasicBlock : IInstructionOperand
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
    /// True, if this instruction has potential side-effects.
    /// </summary>
    public bool HasSideEffects { get; }

    /// <summary>
    /// The values this instruction depends on.
    /// </summary>
    public IEnumerable<Value.Register> Dependencies { get; }

    /// <summary>
    /// The target register, in case the instruction produces a value.
    /// </summary>
    public Value.Register? Target { get; }

    /// <summary>
    /// Retrieves the operand at the given index.
    /// </summary>
    /// <param name="index">The 0-based index to retrieve the operand from.</param>
    /// <returns>The operand at <paramref name="index"/>.</returns>
    public IInstructionOperand this[int index] { get; }
}

/// <summary>
/// A marker type for instruction operands.
/// </summary>
public interface IInstructionOperand
{
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
    /// Arithmetic addition.
    /// </summary>
    Add,

    /// <summary>
    /// Arithmetic subtraction.
    /// </summary>
    Sub,

    /// <summary>
    /// Arithmetic multiplication.
    /// </summary>
    Mul,

    /// <summary>
    /// Arithmetic division.
    /// </summary>
    Div,

    /// <summary>
    /// Arithmetic remainder.
    /// </summary>
    Rem,

    /// <summary>
    /// Less-than comparison.
    /// </summary>
    Less,

    /// <summary>
    /// Equality comparison.
    /// </summary>
    Equal,

    /// <summary>
    /// Arithmetic negation.
    /// </summary>
    Neg,

    /// <summary>
    /// A procedure call.
    /// </summary>
    Call,
}

/// <summary>
/// Base for all values.
/// </summary>
internal abstract partial record class Value : IInstructionOperand
{
    /// <summary>
    /// The <see cref="DracoIr.Type"/> of this <see cref="Value"/>.
    /// </summary>
    public abstract Type Type { get; }
}

internal abstract partial record class Value
{
    /// <summary>
    /// A parameter value.
    /// </summary>
    /// <param name="Type">The type of the parameter.</param>
    /// <param name="Name">The name of the parameter.</param>
    public sealed record class Parameter(Type Type, string Name) : Value
    {
        public override Type Type { get; } = Type;

        public string ToFullString() => $"{this.Type} {this.Name}";
        public override string ToString() => this.Name;
    }

    /// <summary>
    /// A local value.
    /// </summary>
    /// <param name="Type">The type of the local.</param>
    /// <param name="Name">The name of the local.</param>
    public sealed record class Local(Type Type, string Name) : Value
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
internal abstract partial record class Type : IInstructionOperand
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
