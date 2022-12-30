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
    public IReadOnlyList<Parameter> Parameters { get; }

    /// <summary>
    /// The locals in this procedure.
    /// </summary>
    public IReadOnlyList<Local> Locals { get; }

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
/// A procedure parameter.
/// </summary>
/// <param name="Type">The type of the parameter.</param>
/// <param name="Name">The name of the parameter.</param>
internal sealed record class Parameter(Type Type, string Name) : IInstructionOperand
{
    public string ToFullString() => $"{this.Type} {this.Name}";
    public override string ToString() => this.Name;
}

/// <summary>
/// A global, mutable value within an assembly.
/// </summary>
/// <param name="Type">The type of the global.</param>
/// <param name="Name">The name of the global.</param>
internal sealed record class Global(Type Type, string? Name) : IInstructionOperand
{
    public string ToFullString() => $"{this.Type} {this}";
    public override string ToString() => this.Name ?? "<unnamed>";
}

/// <summary>
/// A local, mutable value within a procedure.
/// </summary>
/// <param name="Type">The type of the local.</param>
/// <param name="Name">The name of the local.</param>
internal sealed record class Local(Type Type, string? Name) : IInstructionOperand
{
    public string ToFullString() => $"{this.Type} {this}";
    public override string ToString() => this.Name ?? "<unnamed>";
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
    public IEnumerable<Value.Reg> Dependencies { get; }

    /// <summary>
    /// The target register, in case the instruction produces a value.
    /// </summary>
    public Value.Reg? Target { get; }

    /// <summary>
    /// Retrieves the operand at the given index.
    /// </summary>
    /// <param name="index">The 0-based index to retrieve the operand from.</param>
    /// <returns>The operand at <paramref name="index"/>.</returns>
    public IInstructionOperand this[int index] { get; }

    /// <summary>
    /// The number of operands.
    /// </summary>
    public int OperandCount { get; }
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
    /// <param name="Parameter">The referenced parameter.</param>
    public sealed record class Param(Parameter Parameter) : Value
    {
        public override Type Type => this.Parameter.Type;
    }

    /// <summary>
    /// A register value.
    /// </summary>
    /// <param name="Type">The <see cref="DracoIr.Type"/> of this <see cref="Reg"/>.</param>
    public sealed record class Reg(Type Type) : Value
    {
        private static int idCounter = -1;

        private readonly int id = Interlocked.Increment(ref idCounter);

        public override Type Type { get; } = Type;

        public string ToFullString() => $"{this.Type} {this}";
        public override string ToString() => $"reg_{this.id}";
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
    ///  A constant value.
    /// </summary>
    /// <param name="Value">The constant value.</param>
    public sealed record class Const(object? Value) : Value
    {
        public override Type Type => this.Value switch
        {
            bool => Type.Bool,
            int => Type.Int32,
            _ => throw new InvalidOperationException(),
        };

        public override string ToString() => this.Value?.ToString() ?? "null";
    }

    /// <summary>
    /// A procedure reference.
    /// </summary>
    /// <param name="Procedure">The referenced procedure.</param>
    public sealed record class Proc(IReadOnlyProcedure Procedure) : Value
    {
        public override Type Type => new Type.Proc(
            Args: this.Procedure.Parameters.Select(p => p.Type).ToImmutableArray(),
            Ret: this.Procedure.ReturnType);

        public override string ToString() => this.Procedure.Name;
    }
}

/// <summary>
/// An argument list.
/// </summary>
/// <param name="Values">The values passed in for the call.</param>
internal sealed record class ArgumentList(ImmutableArray<Value> Values) : IInstructionOperand;

/// <summary>
/// Base for all types.
/// </summary>
internal abstract partial record class Type : IInstructionOperand
{
}

// Implementations
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

internal readonly record struct NoOperand : IInstructionOperand;

internal static class InstructionOperandExtensions
{
    public static bool IsNone(this IInstructionOperand operand) => operand is NoOperand;

    public static IReadOnlyBasicBlock AsBlock(this IInstructionOperand operand) => (IReadOnlyBasicBlock)operand;
    public static Local AsLocal(this IInstructionOperand operand) => (Local)operand;
    public static Value AsValue(this IInstructionOperand operand) => (Value)operand;
    public static Type AsType(this IInstructionOperand operand) => (Type)operand;
    public static ArgumentList AsArgumentList(this IInstructionOperand operand) => (ArgumentList)operand;

    public static BasicBlock AsMutableBlock(this IInstructionOperand operand) => (BasicBlock)operand;
}
