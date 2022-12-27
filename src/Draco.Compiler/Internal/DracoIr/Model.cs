using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Utilities;

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

// Implementations /////////////////////////////////////////////////////////////

/// <summary>
/// An <see cref="IReadOnlyAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IReadOnlyAssembly
{
    public string Name { get; set; }

    public IDictionary<string, Procedure> Procedures => this.procedures;
    IReadOnlyDictionary<string, IReadOnlyProcedure> IReadOnlyAssembly.Procedures =>
        new CovariantReadOnlyDictionary<string, Procedure, IReadOnlyProcedure>(this.procedures);

    private readonly Dictionary<string, Procedure> procedures = new();

    public Assembly(string name)
    {
        this.Name = name;
    }

    public Procedure DefineProcedure(string name)
    {
        var proc = new Procedure(name);
        this.Procedures.Add(name, proc);
        return proc;
    }

    public override string ToString() => $"""
        assembly '{this.Name}';

        {string.Join("\n\n", this.Procedures.Values.Select(p => p.ToFullString()))}
        """;
}

/// <summary>
/// An <see cref="IReadOnlyProcedure"/> implementation.
/// </summary>
internal sealed record class Procedure : Value, IReadOnlyProcedure
{
    public override Type Type => new Type.Proc(
        Args: this.Parameters.Select(p => p.Type).ToImmutableArray(),
        Ret: this.ReturnType);

    public string Name { get; }
    public Type ReturnType { get; set; } = Type.Unit;

    public IList<Parameter> Parameters => this.parameters;
    IReadOnlyList<Parameter> IReadOnlyProcedure.Parameters => this.parameters;

    public BasicBlock Entry => this.basicBlocks[0];
    IReadOnlyBasicBlock IReadOnlyProcedure.Entry => this.Entry;

    public IList<BasicBlock> BasicBlocks => this.basicBlocks;
    IReadOnlyList<IReadOnlyBasicBlock> IReadOnlyProcedure.BasicBlocks => this.basicBlocks;

    public IEnumerable<Instruction> Instructions => this.basicBlocks.SelectMany(block => block.Instructions);
    IEnumerable<IReadOnlyInstruction> IReadOnlyProcedure.Instructions => this.Instructions;

    private readonly List<Parameter> parameters = new();
    private readonly List<BasicBlock> basicBlocks = new()
    {
        new(),
    };

    public Procedure(string name)
    {
        this.Name = name;
    }

    public Parameter DefineParameter(string name, Type type)
    {
        var param = new Parameter(type, name, this.parameters.Count);
        this.parameters.Add(param);
        return param;
    }

    /// <summary>
    /// Retrieves the <see cref="InstructionWriter"/> for this procedure.
    /// </summary>
    /// <returns>An <see cref="InstructionWriter"/> that can be used to generate code.</returns>
    public InstructionWriter Writer() => new(this);

    public override string ToString() => this.Name;
    public string ToFullString() => $"""
        proc {this.ReturnType} {this.Name}({string.Join(", ", this.Parameters.Select(p => p.ToFullString()))}):
        {string.Join("\n", this.BasicBlocks.Select(bb => bb.ToFullString()))}
        """;
}

internal sealed class BasicBlock : IReadOnlyBasicBlock
{
    private static int idCounter = -1;

    public IList<Instruction> Instructions => this.instructions;
    IReadOnlyList<IReadOnlyInstruction> IReadOnlyBasicBlock.Instructions => this.instructions;

    private readonly int id = Interlocked.Increment(ref idCounter);
    private readonly List<Instruction> instructions = new();

    public override string ToString() => $"bb_{this.id}";
    public string ToFullString() => $"""
        label {this}:
          {string.Join("\n  ", this.instructions)}
        """;
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

    public override string ToString()
    {
        var result = new StringBuilder();
        var offset = 0;
        // If the first argument is a register, we assume it's a target
        if (this.GetType().GenericTypeArguments.FirstOrDefault() == typeof(Value.Register))
        {
            offset = 1;
            result
                .Append(this.GetOperandAt<Value.Register>(0).ToFullString())
                .Append(" = ");
        }
        result.Append(StringUtils.ToSnakeCase(this.Kind.ToString()));
        for (var i = offset; i < this.OperandCount; ++i)
        {
            if (i == offset) result.Append(' ');
            else result.Append(", ");

            var operand = this.GetOperandAt<object>(i);
            if (operand is IEnumerable<Value> valueList)
            {
                result
                    .Append('[')
                    .AppendJoin(", ", valueList)
                    .Append(']');
            }
            else
            {
                result.Append(operand);
            }
        }
        return result.ToString();
    }
}

// Factory
internal abstract partial class Instruction
{
    public static Instruction Make(InstructionKind kind) => new Instruction0(kind);
    public static Instruction Make<T1>(InstructionKind kind, T1 op1) => new Instruction1<T1>(kind, op1);
    public static Instruction Make<T1, T2>(InstructionKind kind, T1 op1, T2 op2) => new Instruction2<T1, T2>(kind, op1, op2);
    public static Instruction Make<T1, T2, T3>(InstructionKind kind, T1 op1, T2 op2, T3 op3) => new Instruction3<T1, T2, T3>(kind, op1, op2, op3);
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
