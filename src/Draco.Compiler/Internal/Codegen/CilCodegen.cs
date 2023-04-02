using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Types;
using Constant = Draco.Compiler.Internal.OptimizingIr.Model.Constant;
using Parameter = Draco.Compiler.Internal.OptimizingIr.Model.Parameter;
using Void = Draco.Compiler.Internal.OptimizingIr.Model.Void;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates CIL method bodies.
/// </summary>
internal sealed class CilCodegen
{
    /// <summary>
    /// The instruction encoder.
    /// </summary>
    public InstructionEncoder InstructionEncoder { get; set; }

    public IEnumerable<Type> LocalTypes => this.locals
        .Select(kv => (Index: kv.Value, Type: kv.Key.Type))
        .Concat(this.registers.Select(kv => (Index: kv.Value, Type: kv.Key.Type)))
        .OrderBy(p => p.Index)
        .Select(p => p.Type);

    private int NextLocalIndex => this.locals.Count + this.registers.Count;

    private readonly MetadataCodegen metadataCodegen;
    private readonly PdbCodegen? pdbCodegen;
    private readonly IProcedure procedure;
    private readonly Dictionary<IBasicBlock, LabelHandle> labels = new();
    private readonly Dictionary<Local, int> locals = new();
    private readonly Dictionary<Register, int> registers = new();

    public CilCodegen(MetadataCodegen metadataCodegen, PdbCodegen? pdbCodegen, IProcedure procedure)
    {
        this.metadataCodegen = metadataCodegen;
        this.pdbCodegen = pdbCodegen;
        this.procedure = procedure;

        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        this.InstructionEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);
    }

    private FieldDefinitionHandle GetGlobalDefinitionHandle(Global global) => this.metadataCodegen.GetGlobalDefinitionHandle(global);
    private MemberReferenceHandle GetProcedureDefinitionHandle(IProcedure procedure) => this.metadataCodegen.GetProcedureReferenceHandle(procedure);
    private UserStringHandle GetStringLiteralHandle(string text) => this.metadataCodegen.GetStringLiteralHandle(text);
    private MemberReferenceHandle GetIntrinsicHandle(Intrinsic intrinsic) => this.metadataCodegen.GetIntrinsicHandle(intrinsic.Symbol);

    // TODO: Parameters don't handle unit yet, it introduces some signature problems
    private int GetParameterIndex(Parameter parameter) => parameter.Index;
    private int? GetLocalIndex(Local local)
    {
        if (ReferenceEquals(local.Type, IntrinsicTypes.Unit)) return null;
        if (!this.locals.TryGetValue(local, out var index))
        {
            index = this.NextLocalIndex;
            this.locals.Add(local, index);
        }
        return index;
    }
    private int? GetRegisterIndex(Register register)
    {
        if (ReferenceEquals(register.Type, IntrinsicTypes.Unit)) return null;
        if (!this.registers.TryGetValue(register, out var index))
        {
            index = this.NextLocalIndex;
            this.registers.Add(register, index);
        }
        return index;
    }

    private LabelHandle GetLabel(IBasicBlock block)
    {
        if (!this.labels.TryGetValue(block, out var label))
        {
            label = this.InstructionEncoder.DefineLabel();
            this.labels.Add(block, label);
        }
        return label;
    }

    public void EncodeProcedure()
    {
        foreach (var bb in this.procedure.BasicBlocksInDefinitionOrder) this.EncodeBasicBlock(bb);

        this.pdbCodegen?.EncodeProcedure(this.procedure);
    }

    private void EncodeBasicBlock(IBasicBlock basicBlock)
    {
        this.InstructionEncoder.MarkLabel(this.GetLabel(basicBlock));
        foreach (var instr in basicBlock.Instructions) this.EncodeInstruction(instr);
    }

    private void EncodeInstruction(IInstruction instruction)
    {
        switch (instruction)
        {
        case OptimizingIr.Model.SequencePoint sp:
        {
            this.pdbCodegen?.AddSequencePoint(this.InstructionEncoder, sp);
            break;
        }
        case NopInstruction:
        {
            this.InstructionEncoder.OpCode(ILOpCode.Nop);
            break;
        }
        case JumpInstruction jump:
        {
            var target = this.GetLabel(jump.Target);
            this.InstructionEncoder.Branch(ILOpCode.Br, target);
            break;
        }
        case BranchInstruction branch:
        {
            this.EncodePush(branch.Condition);
            var then = this.GetLabel(branch.Then);
            var @else = this.GetLabel(branch.Else);
            this.InstructionEncoder.Branch(ILOpCode.Brtrue, then);
            this.InstructionEncoder.Branch(ILOpCode.Br, @else);
            break;
        }
        case RetInstruction ret:
        {
            this.EncodePush(ret.Value);
            this.InstructionEncoder.OpCode(ILOpCode.Ret);
            break;
        }
        case ArithmeticInstruction arithmetic:
        {
            this.EncodePush(arithmetic.Left);
            this.EncodePush(arithmetic.Right);
            this.InstructionEncoder.OpCode(arithmetic.Op switch
            {
                ArithmeticOp.Add => ILOpCode.Add,
                ArithmeticOp.Sub => ILOpCode.Sub,
                ArithmeticOp.Mul => ILOpCode.Mul,
                ArithmeticOp.Div => ILOpCode.Div,
                ArithmeticOp.Rem => ILOpCode.Rem,
                ArithmeticOp.Less => ILOpCode.Clt,
                ArithmeticOp.Equal => ILOpCode.Ceq,
                _ => throw new System.InvalidOperationException(),
            });
            this.StoreLocal(arithmetic.Target);
            break;
        }
        case LoadInstruction load:
        {
            // Depends on where we load from
            switch (load.Source)
            {
            case Local local:
                this.LoadLocal(local);
                break;
            case Global global:
                this.InstructionEncoder.OpCode(ILOpCode.Ldsfld);
                this.InstructionEncoder.Token(this.GetGlobalDefinitionHandle(global));
                break;
            default:
                throw new System.InvalidOperationException();
            }
            // Just copy to the target local
            this.StoreLocal(load.Target);
            break;
        }
        case StoreInstruction store:
        {
            this.EncodePush(store.Source);
            // Depends on where we store to
            switch (store.Target)
            {
            case Local local:
                this.StoreLocal(local);
                break;
            case Global global:
                this.InstructionEncoder.OpCode(ILOpCode.Stsfld);
                this.InstructionEncoder.Token(this.GetGlobalDefinitionHandle(global));
                break;
            default:
                throw new System.InvalidOperationException();
            }
            break;
        }
        case CallInstruction call:
        {
            // Arguments
            foreach (var arg in call.Arguments) this.EncodePush(arg);
            // Determine what we are calling
            if (call.Procedure is IProcedure proc)
            {
                // Regular procedure call
                var handle = this.GetProcedureDefinitionHandle(proc);
                this.InstructionEncoder.Call(handle);
            }
            else if (call.Procedure is Intrinsic intrinsic)
            {
                var handle = this.GetIntrinsicHandle(intrinsic);
                this.InstructionEncoder.Call(handle);
            }
            else
            {
                // TODO
                throw new System.NotImplementedException();
            }
            // Store result
            this.StoreLocal(call.Target);
            break;
        }
        default:
            throw new System.ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private void EncodePush(IOperand operand)
    {
        switch (operand)
        {
        case Void:
            return;
        case Register r:
            this.LoadLocal(r);
            break;
        case Parameter p:
            this.InstructionEncoder.LoadArgument(this.GetParameterIndex(p));
            break;
        case Constant c:
            switch (c.Value)
            {
            case int i:
                this.InstructionEncoder.LoadConstantI4(i);
                break;
            case bool b:
                this.InstructionEncoder.LoadConstantI4(b ? 1 : 0);
                break;
            case string s:
                var stringHandle = this.GetStringLiteralHandle(s);
                this.InstructionEncoder.LoadString(stringHandle);
                break;
            default:
                throw new System.NotImplementedException();
            }
            break;
        default:
            throw new System.ArgumentOutOfRangeException(nameof(operand));
        }
    }

    private void LoadLocal(Local local)
    {
        var index = this.GetLocalIndex(local);
        if (index is null) return;
        this.InstructionEncoder.LoadLocal(index.Value);
    }

    private void LoadLocal(Register register)
    {
        var index = this.GetRegisterIndex(register);
        if (index is null) return;
        this.InstructionEncoder.LoadLocal(index.Value);
    }

    private void StoreLocal(Local local)
    {
        var index = this.GetLocalIndex(local);
        if (index is null) return;
        this.InstructionEncoder.StoreLocal(index.Value);
    }

    private void StoreLocal(Register register)
    {
        var index = this.GetRegisterIndex(register);
        if (index is null) return;
        this.InstructionEncoder.StoreLocal(index.Value);
    }
}
