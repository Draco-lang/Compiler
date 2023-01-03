using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Internal.DracoIr;

/// <summary>
/// An <see cref="IReadOnlyAssembly"/> implementation.
/// </summary>
internal sealed class Assembly : IReadOnlyAssembly
{
    public string Name { get; set; }

    public IList<Global> Globals => this.globals;
    IReadOnlyList<Global> IReadOnlyAssembly.Globals => this.globals;

    public Procedure? EntryPoint { get; set; }
    IReadOnlyProcedure? IReadOnlyAssembly.EntryPoint => this.EntryPoint;

    public Procedure GlobalInitializer { get; } = new("@GlobalInitializer");
    IReadOnlyProcedure IReadOnlyAssembly.GlobalInitializer => this.GlobalInitializer;

    public IList<Procedure> Procedures => this.procedures;
    IReadOnlyList<IReadOnlyProcedure> IReadOnlyAssembly.Procedures => this.procedures;

    private readonly List<Global> globals = new();
    private readonly List<Procedure> procedures = new();

    public Assembly(string name)
    {
        this.Name = name;
    }

    public Global DefineGlobal(string name, Type type)
    {
        var global = new Global(type, name);
        if (type != Type.Unit) this.globals.Add(global);
        return global;
    }

    public Procedure DefineProcedure(string name)
    {
        var proc = new Procedure(name);
        this.Procedures.Add(proc);
        return proc;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        result.AppendLine($"assembly '{this.Name}';");
        if (this.EntryPoint is not null) result.AppendLine($"entry-point: {this.EntryPoint};");
        result.Append(string.Join(Environment.NewLine, this.Procedures.Select(p => p.ToFullString())));
        return result.ToString();
    }
}

/// <summary>
/// An <see cref="IReadOnlyProcedure"/> implementation.
/// </summary>
internal sealed record class Procedure : IReadOnlyProcedure
{
    public string Name { get; }
    public Type ReturnType { get; set; } = Type.Unit;

    public IList<Parameter> Parameters => this.parameters;
    IReadOnlyList<Parameter> IReadOnlyProcedure.Parameters => this.parameters;

    public IList<Local> Locals => this.locals;
    IReadOnlyList<Local> IReadOnlyProcedure.Locals => this.locals;

    public BasicBlock Entry => this.basicBlocks[0];
    IReadOnlyBasicBlock IReadOnlyProcedure.Entry => this.Entry;

    public IList<BasicBlock> BasicBlocks => this.basicBlocks;
    IReadOnlyList<IReadOnlyBasicBlock> IReadOnlyProcedure.BasicBlocks => this.basicBlocks;

    public IEnumerable<Instruction> Instructions => this.basicBlocks.SelectMany(block => block.Instructions);
    IEnumerable<IReadOnlyInstruction> IReadOnlyProcedure.Instructions => this.Instructions;

    private readonly List<Parameter> parameters = new();
    private readonly List<Local> locals = new();
    private readonly List<BasicBlock> basicBlocks;

    public Procedure(string name)
    {
        this.Name = name;
        this.basicBlocks = new()
        {
            new(this),
        };
    }

    public Parameter DefineParameter(string name, Type type)
    {
        var param = new Parameter(type, name);
        this.parameters.Add(param);
        return param;
    }

    public Local DefineLocal(string? name, Type type)
    {
        var local = new Local(type, name);
        if (type != Type.Unit) this.locals.Add(local);
        return local;
    }

    /// <summary>
    /// Retrieves the <see cref="InstructionWriter"/> for this procedure.
    /// </summary>
    /// <returns>An <see cref="InstructionWriter"/> that can be used to generate code.</returns>
    public InstructionWriter Writer() => new(this);

    public override string ToString() => this.Name;
    public string ToFullString() => $"""
        proc {this.ReturnType} {this.Name}({string.Join(", ", this.Parameters.Select(p => p.ToFullString()))}):
        locals (
        {string.Join(",\n", this.Locals.Select(loc => $"  {loc.ToFullString()}"))})
        {string.Join("\n", this.BasicBlocks.Select(bb => bb.ToFullString()))}
        """;
}

internal sealed class BasicBlock : IReadOnlyBasicBlock
{
    private static int idCounter = -1;

    public Procedure Procedure { get; }
    IReadOnlyProcedure IReadOnlyBasicBlock.Procedure => this.Procedure;

    public IList<Instruction> Instructions => this.instructions;
    IReadOnlyList<IReadOnlyInstruction> IReadOnlyBasicBlock.Instructions => this.instructions;

    private readonly int id = Interlocked.Increment(ref idCounter);
    private readonly List<Instruction> instructions = new();

    public BasicBlock(Procedure procedure)
    {
        this.Procedure = procedure;
    }

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

    private static readonly InstructionKind[] sideEffectInstructions = new[]
    {
        InstructionKind.Call,
        InstructionKind.Store,
    };

    public InstructionKind Kind { get; }
    public Value.Reg? Target { get; }
    public IEnumerable<Value.Reg> Dependencies
    {
        get
        {
            for (var i = 0; i < this.OperandCount; ++i)
            {
                var op = this[i];
                if (op is Value.Reg r) yield return r;
                if (op is ArgumentList l)
                {
                    foreach (var v in l.Values)
                    {
                        if (v is Value.Reg r2) yield return r2;
                    }
                }
            }
        }
    }
    public abstract IInstructionOperand this[int index] { get; set; }

    public bool IsBranch => branchInstructions.Contains(this.Kind);
    public bool HasSideEffects => sideEffectInstructions.Contains(this.Kind);
    public int OperandCount => this.Kind switch
    {
        InstructionKind.Nop => 0,
        InstructionKind.Store => 2,
        InstructionKind.Load => 1,
        InstructionKind.Ret => 1,
        InstructionKind.Jmp => 1,
        InstructionKind.JmpIf => 3,
        InstructionKind.Add => 2,
        InstructionKind.Sub => 2,
        InstructionKind.Mul => 2,
        InstructionKind.Div => 2,
        InstructionKind.Rem => 2,
        InstructionKind.Less => 2,
        InstructionKind.Equal => 2,
        InstructionKind.Neg => 1,
        InstructionKind.Call => 2,
        _ => throw new InvalidOperationException(),
    };

    protected Instruction(InstructionKind kind, Value.Reg? target)
    {
        this.Kind = kind;
        this.Target = target;
    }

    public override string ToString()
    {
        var result = new StringBuilder();
        if (this.Target is not null) result.Append($"{this.Target.ToFullString()} = ");
        result.Append(StringUtils.ToSnakeCase(this.Kind.ToString()));
        if (this.OperandCount > 0)
        {
            result.Append(' ');
            result.Append(string.Join(", ", Enumerable.Range(0, this.OperandCount).Select(i => this[i])));
        }
        return result.ToString();
    }
}

// Factory
internal abstract partial class Instruction
{
    public static Instruction Make0(InstructionKind kind) =>
        Make(kind, null, default(NoOperand), default(NoOperand), default(NoOperand));
    public static Instruction Make1<T1>(InstructionKind kind, T1 op1)
        where T1 : IInstructionOperand => Make(kind, null, op1, default(NoOperand), default(NoOperand));
    public static Instruction Make2<T1, T2>(InstructionKind kind, T1 op1, T2 op2)
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand => Make(kind, null, op1, op2, default(NoOperand));
    public static Instruction Make3<T1, T2, T3>(InstructionKind kind, T1 op1, T2 op2, T3 op3)
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand
        where T3 : IInstructionOperand => Make(kind, null, op1, op2, op3);

    public static Instruction Make0(InstructionKind kind, Value.Reg target) =>
        Make(kind, target, default(NoOperand), default(NoOperand), default(NoOperand));
    public static Instruction Make1<T1>(InstructionKind kind, Value.Reg target, T1 op1)
        where T1 : IInstructionOperand => Make(kind, target, op1, default(NoOperand), default(NoOperand));
    public static Instruction Make2<T1, T2>(InstructionKind kind, Value.Reg target, T1 op1, T2 op2)
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand => Make(kind, target, op1, op2, default(NoOperand));
    public static Instruction Make3<T1, T2, T3>(InstructionKind kind, Value.Reg target, T1 op1, T2 op2, T3 op3)
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand
        where T3 : IInstructionOperand => Make(kind, target, op1, op2, op3);

    private static Instruction Make<T1, T2, T3>(InstructionKind kind, Value.Reg? target, T1 op1, T2 op2, T3 op3)
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand
        where T3 : IInstructionOperand => new Impl<T1, T2, T3>(kind, target, op1, op2, op3);
}

// Implementation
internal abstract partial class Instruction
{
    private sealed class Impl<T1, T2, T3> : Instruction
        where T1 : IInstructionOperand
        where T2 : IInstructionOperand
        where T3 : IInstructionOperand
    {
        public override IInstructionOperand this[int index]
        {
            get => index switch
            {
                0 => this.op1,
                1 => this.op2,
                2 => this.op3,
                _ => default(NoOperand),
            };
            set
            {
                switch (index)
                {
                case 0:
                    if (value is not T1 v1) throw new ArgumentException(nameof(value));
                    this.op1 = v1;
                    break;
                case 1:
                    if (value is not T2 v2) throw new ArgumentException(nameof(value));
                    this.op2 = v2;
                    break;
                case 2:
                    if (value is not T3 v3) throw new ArgumentException(nameof(value));
                    this.op3 = v3;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }
        }

        private T1 op1;
        private T2 op2;
        private T3 op3;

        public Impl(InstructionKind kind, Value.Reg? target, T1 op1, T2 op2, T3 op3)
            : base(kind, target)
        {
            this.op1 = op1;
            this.op2 = op2;
            this.op3 = op3;
        }
    }
}
