using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
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
    public InstructionEncoder InstructionEncoder { get; }

    /// <summary>
    /// The allocated locals in order.
    /// </summary>
    public IEnumerable<AllocatedLocal> AllocatedLocals => this.allocatedLocals
        .OrderBy(kv => kv.Value.Index)
        .Select(kv => kv.Value);

    private PdbCodegen? PdbCodegen => this.metadataCodegen.PdbCodegen;

    private readonly MetadataCodegen metadataCodegen;
    private readonly IProcedure procedure;
    private readonly Dictionary<IBasicBlock, LabelHandle> labels = new();
    private readonly Dictionary<IOperand, AllocatedLocal> allocatedLocals = new();

    public CilCodegen(MetadataCodegen metadataCodegen, IProcedure procedure)
    {
        this.metadataCodegen = metadataCodegen;
        this.procedure = procedure;

        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        this.InstructionEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);
    }

    private MemberReferenceHandle GetGlobalReferenceHandle(Global global) => this.metadataCodegen.GetGlobalReferenceHandle(global);
    private MemberReferenceHandle GetProcedureDefinitionHandle(IProcedure procedure) => this.metadataCodegen.GetProcedureReferenceHandle(procedure);
    private UserStringHandle GetStringLiteralHandle(string text) => this.metadataCodegen.GetStringLiteralHandle(text);
    private TypeReferenceHandle GetTypeReferenceHandle(Symbol symbol) => this.metadataCodegen.GetTypeReferenceHandle(symbol);
    private MemberReferenceHandle GetMemberReferenceHandle(Symbol symbol) => this.metadataCodegen.GetMemberReferenceHandle(symbol);

    // TODO: Parameters don't handle unit yet, it introduces some signature problems
    private int GetParameterIndex(Parameter parameter) => parameter.Index;

    private AllocatedLocal? GetAllocatedLocal(IOperand operand)
    {
        if (ReferenceEquals(operand.Type, IntrinsicSymbols.Unit)) return null;
        if (!this.allocatedLocals.TryGetValue(operand, out var local))
        {
            local = new(operand, this.allocatedLocals.Count);
            this.allocatedLocals.Add(operand, local);
        }
        return local;
    }

    private int? GetLocalIndex(Local local) => this.GetAllocatedLocal(local)?.Index;
    private int? GetRegisterIndex(Register register) => this.GetAllocatedLocal(register)?.Index;

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
            this.PdbCodegen?.AddSequencePoint(this.InstructionEncoder, sp);
            break;
        }
        case StartScope start:
        {
            var localIndices = start.Locals
                .Select(sym => this.procedure.Locals[sym])
                .Select(loc => this.GetAllocatedLocal(loc))
                .OfType<AllocatedLocal>();
            this.PdbCodegen?.StartScope(this.InstructionEncoder.Offset, localIndices);
            break;
        }
        case EndScope:
        {
            this.PdbCodegen?.EndScope(this.InstructionEncoder.Offset);
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
                _ => throw new InvalidOperationException(),
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
                this.InstructionEncoder.Token(this.GetGlobalReferenceHandle(global));
                break;
            default:
                throw new InvalidOperationException();
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
                this.InstructionEncoder.Token(this.GetGlobalReferenceHandle(global));
                break;
            default:
                throw new InvalidOperationException();
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
            else if (call.Procedure is MetadataReference metadataRef)
            {
                switch (metadataRef.Symbol)
                {
                case SynthetizedMetadataConstructorSymbol ctor:
                {
                    // The constructed type is from metadata, but the ctor symbol doesn't contain the real names
                    var type = this.GetTypeReferenceHandle(ctor.ReturnType);
                    // TODO: Not cached
                    var handle = this.metadataCodegen.AddMemberReference(
                        type: type,
                        name: ".ctor",
                        signature: this.metadataCodegen.EncodeBlob(e =>
                        {
                            // TODO: Don't we want to have a utility in MetadataCodegen instead?
                            var funcType = (FunctionTypeSymbol)ctor.Type;
                            e
                                .MethodSignature(isInstanceMethod: true)
                                .Parameters(funcType.Parameters.Length, out var returnType, out var parameters);
                            // Ctors always have void return-type
                            returnType.Void();
                            foreach (var param in funcType.Parameters)
                            {
                                this.metadataCodegen.EncodeSignatureType(parameters.AddParameter().Type(), param.Type);
                            }
                        }));
                    // Encode instantiation
                    this.InstructionEncoder.OpCode(ILOpCode.Newobj);
                    this.InstructionEncoder.Token(handle);
                    break;
                }
                case SynthetizedArrayFunctionSymbol arr:
                {
                    if (arr.ArrayType.Rank != 1)
                    {
                        // TODO: Only support SZ arrays for now.
                        throw new NotImplementedException();
                    }

                    var elementTypeRef = this.GetTypeReferenceHandle(arr.ArrayType.ElementType);
                    switch (arr.Kind)
                    {
                    case SynthetizedArrayFunctionSymbol.FunctionKind.Constructor:
                        this.InstructionEncoder.OpCode(ILOpCode.Newarr);
                        this.InstructionEncoder.Token(elementTypeRef);
                        break;
                    default:
                        // I'm not sure what else can get here
                        throw new UnreachableException();
                    }
                    break;
                }
                default:
                {
                    // Regular lookup
                    var handle = this.GetMemberReferenceHandle(metadataRef.Symbol);
                    this.InstructionEncoder.Call(handle);
                    break;
                }
                }
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }
            // Store result
            this.StoreLocal(call.Target);
            break;
        }
        case MemberCallInstruction mcall:
        {
            // Receiver
            this.EncodePush(mcall.Receiver);
            // Arguments
            foreach (var arg in mcall.Arguments) this.EncodePush(arg);
            // Determine what we are calling
            if (mcall.Procedure is MetadataReference metadataRef)
            {
                var symbol = (FunctionSymbol)metadataRef.Symbol;
                if (symbol is SynthetizedArrayFunctionSymbol arr)
                {
                    var elementTypeRef = this.GetTypeReferenceHandle(arr.ArrayType.ElementType);
                    switch (arr.Kind)
                    {
                    case SynthetizedArrayFunctionSymbol.FunctionKind.Get:
                        this.InstructionEncoder.OpCode(ILOpCode.Ldelem);
                        this.InstructionEncoder.Token(elementTypeRef);
                        break;
                    case SynthetizedArrayFunctionSymbol.FunctionKind.Set:
                        this.InstructionEncoder.OpCode(ILOpCode.Stelem);
                        this.InstructionEncoder.Token(elementTypeRef);
                        break;
                    }
                }
                else
                {
                    var handle = this.GetMemberReferenceHandle(metadataRef.Symbol);
                    this.InstructionEncoder.OpCode(symbol.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call);
                    this.InstructionEncoder.Token(handle);
                }
            }
            else
            {
                // TODO
                throw new NotImplementedException();
            }
            // Store result
            this.StoreLocal(mcall.Target);
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
                throw new NotImplementedException();
            }
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(operand));
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
