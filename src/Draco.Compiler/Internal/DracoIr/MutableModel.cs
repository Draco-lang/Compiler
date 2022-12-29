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
