using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Constant = Draco.Compiler.Internal.OptimizingIr.Model.Constant;
using Parameter = Draco.Compiler.Internal.OptimizingIr.Model.Parameter;
using Void = Draco.Compiler.Internal.OptimizingIr.Model.Void;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates CIL method bodies.
/// </summary>
internal sealed class CilCodegen
{
    public static InstructionEncoder GenerateProcedureBody(MetadataCodegen metadataCodegen, IProcedure procedure)
    {
        var cilCodegen = new CilCodegen(metadataCodegen, procedure);
        cilCodegen.EncodeProcedure();
        return cilCodegen.encoder;
    }

    private readonly MetadataCodegen metadataCodegen;
    private readonly IProcedure procedure;
    private readonly InstructionEncoder encoder;
    private readonly Dictionary<IBasicBlock, LabelHandle> labels = new();

    private CilCodegen(MetadataCodegen metadataCodegen, IProcedure procedure)
    {
        this.metadataCodegen = metadataCodegen;
        this.procedure = procedure;

        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        this.encoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);
    }

    private FieldDefinitionHandle GetGlobalDefinitionHandle(Global global) => this.metadataCodegen.GetGlobalDefinitionHandle(global);
    private MethodDefinitionHandle GetProcedureDefinitionHandle(IProcedure procedure) => throw new NotImplementedException();
    private UserStringHandle GetStringLiteralHandle(string text) => this.metadataCodegen.GetStringLiteralHandle(text);

    private int GetParameterIndex(Parameter parameter) => parameter.Index;
    private int GetLocalIndex(Local local) => local.Index;
    private int GetRegisterIndex(Register register) =>
        this.procedure.Locals.Count + register.Index;

    private LabelHandle GetLabel(IBasicBlock block)
    {
        if (!this.labels.TryGetValue(block, out var label))
        {
            label = this.encoder.DefineLabel();
            this.labels.Add(block, label);
        }
        return label;
    }

    private void EncodeProcedure()
    {
        foreach (var bb in this.procedure.BasicBlocksInDefinitionOrder) this.EncodeBasicBlock(bb);
    }

    private void EncodeBasicBlock(IBasicBlock basicBlock)
    {
        this.encoder.MarkLabel(this.GetLabel(basicBlock));
        foreach (var instr in basicBlock.Instructions) this.EncodeInstruction(instr);
    }

    private void EncodeInstruction(IInstruction instruction)
    {
        switch (instruction)
        {
        case NopInstruction:
        {
            this.encoder.OpCode(ILOpCode.Nop);
            break;
        }
        case JumpInstruction jump:
        {
            var target = this.GetLabel(jump.Target);
            this.encoder.Branch(ILOpCode.Br, target);
            break;
        }
        case BranchInstruction branch:
        {
            this.EncodePush(branch.Condition);
            var then = this.GetLabel(branch.Then);
            var @else = this.GetLabel(branch.Else);
            this.encoder.Branch(ILOpCode.Brtrue, then);
            this.encoder.Branch(ILOpCode.Br, @else);
            break;
        }
        case RetInstruction ret:
        {
            this.EncodePush(ret.Value);
            this.encoder.OpCode(ILOpCode.Ret);
            break;
        }
        case ArithmeticInstruction arithmetic:
        {
            this.EncodePush(arithmetic.Left);
            this.EncodePush(arithmetic.Right);
            this.encoder.OpCode(arithmetic.Op switch
            {
                ArithmeticOp.Add => ILOpCode.Add,
                ArithmeticOp.Sub => ILOpCode.Sub,
                ArithmeticOp.Mul => ILOpCode.Mul,
                ArithmeticOp.Div => ILOpCode.Div,
                ArithmeticOp.Rem => ILOpCode.Rem,
                ArithmeticOp.Less => ILOpCode.Clt,
                ArithmeticOp.Equal => ILOpCode.Ceq,
                _ => throw new InvalidOperationException(),
            });
            var result = this.GetRegisterIndex(arithmetic.Target);
            this.encoder.StoreLocal(result);
            break;
        }
        case LoadInstruction load:
        {
            // Depends on where we load from
            switch (load.Source)
            {
            case Local local:
                this.encoder.LoadLocal(this.GetLocalIndex(local));
                break;
            case Global global:
                this.encoder.OpCode(ILOpCode.Ldsfld);
                this.encoder.Token(this.GetGlobalDefinitionHandle(global));
                break;
            default:
                throw new InvalidOperationException();
            }
            // Just copy to the target local
            var result = this.GetRegisterIndex(load.Target);
            this.encoder.StoreLocal(result);
            break;
        }
        case StoreInstruction store:
        {
            this.EncodePush(store.Source);
            // Depends on where we store to
            switch (store.Target)
            {
            case Local local:
                this.encoder.StoreLocal(this.GetLocalIndex(local));
                break;
            case Global global:
                this.encoder.OpCode(ILOpCode.Stsfld);
                this.encoder.Token(this.GetGlobalDefinitionHandle(global));
                break;
            default:
                throw new InvalidOperationException();
            }
            break;
        }
        case CallInstruction call:
        {
            if (call.Procedure is IProcedure proc)
            {
                // Regular procedure call
                // Arguments
                foreach (var arg in call.Arguments) this.EncodePush(arg);
                // Called procedure
                var handle = this.GetProcedureDefinitionHandle(proc);
                this.encoder.Call(handle);
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }
            break;
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private void EncodePush(IOperand operand)
    {
        switch (operand)
        {
        case Void:
            return;
        case Register r:
            this.encoder.LoadLocal(this.GetRegisterIndex(r));
            break;
        case Parameter p:
            this.encoder.LoadArgument(this.GetParameterIndex(p));
            break;
        case Constant c:
            switch (c.Value)
            {
            case int i:
                this.encoder.LoadConstantI4(i);
                break;
            case bool b:
                this.encoder.LoadConstantI4(b ? 1 : 0);
                break;
            case string s:
                var stringHandle = this.GetStringLiteralHandle(s);
                this.encoder.LoadString(stringHandle);
                break;
            default:
                throw new NotImplementedException();
            }
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(operand));
        }
    }
}
