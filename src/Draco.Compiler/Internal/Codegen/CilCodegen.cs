using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Draco.Compiler.Internal.OptimizingIr;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Constant = Draco.Compiler.Internal.OptimizingIr.Model.Constant;
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

    /// <summary>
    /// The allocated registers in order.
    /// </summary>
    public IEnumerable<Register> AllocatedRegisters => this.allocatedRegisters
        .OrderBy(kv => kv.Value)
        .Select(kv => kv.Key);

    private PdbCodegen? PdbCodegen => this.metadataCodegen.PdbCodegen;

    private readonly MetadataCodegen metadataCodegen;
    private readonly IProcedure procedure;
    private readonly ImmutableDictionary<LocalSymbol, AllocatedLocal> allocatedLocals;
    private readonly ImmutableDictionary<Register, int> allocatedRegisters;
    private readonly Dictionary<IBasicBlock, LabelHandle> labels = new();

    // NOTE: The current stackification attempt is FLAWED
    // Imagine this situation:
    //
    // r1 := load loc0
    // r2 := box 1 as object
    // store r1[0] := r2
    //
    // We stackify it and get
    //
    // load loc0
    // box 1 as object
    // store r1[0]
    //
    // OOPS! The index "leaked behind" the value, reversing the order
    // We might need to structure the instructions in a tree after all

    public CilCodegen(MetadataCodegen metadataCodegen, IProcedure procedure)
    {
        this.metadataCodegen = metadataCodegen;
        this.procedure = procedure;

        var codeBuilder = new BlobBuilder();
        var controlFlowBuilder = new ControlFlowBuilder();
        this.InstructionEncoder = new InstructionEncoder(codeBuilder, controlFlowBuilder);

        this.allocatedLocals = procedure.Locals
            .Where(local => !SymbolEqualityComparer.Default.Equals(local.Type, IntrinsicSymbols.Unit))
            .Select((local, index) => (Local: local, Index: index))
            .ToImmutableDictionary(pair => pair.Local, pair => new AllocatedLocal(pair.Local, pair.Index));
        this.allocatedRegisters = procedure.Registers
            .Where(reg => !SymbolEqualityComparer.Default.Equals(reg.Type, IntrinsicSymbols.Unit))
            .Select((reg, index) => (Register: reg, Index: index))
            .ToImmutableDictionary(pair => pair.Register, pair => this.allocatedLocals.Count + pair.Index);
    }

    private UserStringHandle GetStringLiteralHandle(string text) => this.metadataCodegen.GetStringLiteralHandle(text);

    private EntityHandle GetHandle(Symbol symbol) => this.metadataCodegen.GetEntityHandle(symbol);

    // TODO: Parameters don't handle unit yet, it introduces some signature problems
    private int GetParameterIndex(ParameterSymbol parameter) => this.procedure.GetParameterIndex(parameter);

    private AllocatedLocal? GetAllocatedLocal(LocalSymbol local)
    {
        if (!this.allocatedLocals.TryGetValue(local, out var allocatedLocal)) return null;
        return allocatedLocal;
    }

    private int? GetLocalIndex(LocalSymbol local) => this.GetAllocatedLocal(local)?.Index;
    private int? GetRegisterIndex(Register register)
    {
        if (!this.allocatedRegisters.TryGetValue(register, out var allocatedRegister)) return null;
        return allocatedRegister;
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
                .Select(sym => this.GetAllocatedLocal(sym))
                .OfType<AllocatedLocal>();
            this.PdbCodegen?.StartScope(this.InstructionEncoder.Offset, this.allocatedLocals.Values);
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
            case ParameterSymbol param:
            {
                var index = this.GetParameterIndex(param);
                this.InstructionEncoder.LoadArgument(index);
                break;
            }
            case LocalSymbol local:
            {
                this.LoadLocal(local);
                break;
            }
            case GlobalSymbol global:
            {
                this.InstructionEncoder.OpCode(ILOpCode.Ldsfld);
                this.EncodeToken(global);
                break;
            }
            default:
                throw new InvalidOperationException();
            }
            // Just copy to the target local
            this.StoreLocal(load.Target);
            break;
        }
        case LoadElementInstruction loadElement:
        {
            // Array
            this.EncodePush(loadElement.Array);
            // Indices
            foreach (var i in loadElement.Indices) this.EncodePush(i);
            // One-dimensional and multi-dimensional arrays are very different
            if (loadElement.Indices.Count == 1)
            {
                // One-dimensional
                this.InstructionEncoder.OpCode(ILOpCode.Ldelem);
                this.EncodeToken(loadElement.Target.Type);
            }
            else
            {
                // Multi-dimensional
                this.InstructionEncoder.OpCode(ILOpCode.Call);
                this.InstructionEncoder.Token(this.metadataCodegen.GetMultidimensionalArrayGetHandle(
                    loadElement.Target.Type,
                    loadElement.Indices.Count));
            }
            // Store result
            this.StoreLocal(loadElement.Target);
            break;
        }
        case LoadFieldInstruction loadField:
        {
            this.EncodePush(loadField.Receiver);
            this.InstructionEncoder.OpCode(ILOpCode.Ldfld);
            this.InstructionEncoder.Token(this.GetHandle(loadField.Member));
            this.StoreLocal(loadField.Target);
            break;
        }
        case StoreInstruction store:
        {
            switch (store.Target)
            {
            case ParameterSymbol:
                throw new InvalidOperationException();
            case LocalSymbol local:
                this.EncodePush(store.Source);
                this.StoreLocal(local);
                break;
            case GlobalSymbol global:
                this.EncodePush(store.Source);
                this.InstructionEncoder.OpCode(ILOpCode.Stsfld);
                this.EncodeToken(global);
                break;
            default:
                throw new InvalidOperationException();
            }
            break;
        }
        case StoreElementInstruction storeElement:
        {
            this.EncodePush(storeElement.TargetArray);
            foreach (var index in storeElement.Indices) this.EncodePush(index);
            this.EncodePush(storeElement.Source);

            // TODO: Not the prettiest...
            var targetStorageType = storeElement.TargetArray.Type!.Substitution.GenericArguments[0].Substitution;

            if (storeElement.Indices.Count == 1)
            {
                // One-dimensional array
                this.InstructionEncoder.OpCode(ILOpCode.Stelem);
                this.EncodeToken(targetStorageType);
            }
            else
            {
                // Multi-dimensional array
                this.InstructionEncoder.OpCode(ILOpCode.Call);
                this.InstructionEncoder.Token(this.metadataCodegen.GetMultidimensionalArraySetHandle(
                    targetStorageType,
                    storeElement.Indices.Count));
            }
            break;
        }
        case StoreFieldInstruction storeField:
        {
            this.EncodePush(storeField.Receiver);
            this.EncodePush(storeField.Source);
            this.InstructionEncoder.OpCode(ILOpCode.Stfld);
            this.InstructionEncoder.Token(this.GetHandle(storeField.Member));
            break;
        }
        case AddressOfInstruction addressOf:
        {
            switch (addressOf.Source)
            {
            case ParameterSymbol param:
            {
                var paramIndex = this.GetParameterIndex(param);
                this.InstructionEncoder.LoadArgumentAddress(paramIndex);
                this.StoreLocal(addressOf.Target);
                break;
            }
            case LocalSymbol local:
            {
                var localIndex = this.GetLocalIndex(local);
                // NOTE: What if we ask the address of a unit?
                Debug.Assert(localIndex is not null);
                this.InstructionEncoder.LoadLocalAddress(localIndex.Value);
                this.StoreLocal(addressOf.Target);
                break;
            }
            case GlobalSymbol global:
            {
                this.InstructionEncoder.OpCode(ILOpCode.Ldsflda);
                this.EncodeToken(global);
                this.StoreLocal(addressOf.Target);
                break;
            }
            default:
                throw new InvalidOperationException();
            }
            break;
        }
        case CallInstruction call:
        {
            // Arguments
            foreach (var arg in call.Arguments) this.EncodePush(arg);
            // Call
            this.InstructionEncoder.OpCode(ILOpCode.Call);
            this.EncodeToken(call.Procedure);
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
            // Call
            this.InstructionEncoder.OpCode(mcall.Procedure.IsVirtual ? ILOpCode.Callvirt : ILOpCode.Call);
            this.EncodeToken(mcall.Procedure);
            // Store result
            this.StoreLocal(mcall.Target);
            break;
        }
        case NewObjectInstruction newObj:
        {
            // Arguments
            foreach (var arg in newObj.Arguments) this.EncodePush(arg);
            this.InstructionEncoder.OpCode(ILOpCode.Newobj);
            this.EncodeToken(newObj.Constructor);
            // Store result
            this.StoreLocal(newObj.Target);
            break;
        }
        case NewArrayInstruction newArr:
        {
            // Dimensions
            foreach (var dim in newArr.Dimensions) this.EncodePush(dim);
            // One-dimensional and multi-dimensional arrays are very different
            if (newArr.Dimensions.Count == 1)
            {
                // One-dimensional
                this.InstructionEncoder.OpCode(ILOpCode.Newarr);
                this.EncodeToken(newArr.ElementType);
            }
            else
            {
                // Multi-dimensional
                this.InstructionEncoder.OpCode(ILOpCode.Newobj);
                this.InstructionEncoder.Token(this.metadataCodegen.GetMultidimensionalArrayCtorHandle(
                    newArr.ElementType,
                    newArr.Dimensions.Count));
            }
            // Store result
            this.StoreLocal(newArr.Target);
            break;
        }
        case ArrayLengthInstruction arrLen:
        {
            // Array
            this.EncodePush(arrLen.Array);
            // Length query
            this.InstructionEncoder.OpCode(ILOpCode.Ldlen);
            // Convert to I4
            this.InstructionEncoder.OpCode(ILOpCode.Conv_i4);
            // Store result
            this.StoreLocal(arrLen.Target);
            break;
        }
        case BoxInstruction box:
        {
            // Value to be boxed
            this.EncodePush(box.Value);
            // Box it
            this.InstructionEncoder.OpCode(ILOpCode.Box);
            this.EncodeToken(box.Value.Type!);
            // Store result
            this.StoreLocal(box.Target);
            break;
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(instruction));
        }
    }

    private void EncodeToken(Symbol symbol)
    {
        var handle = this.GetHandle(symbol);
        this.InstructionEncoder.Token(handle);
    }

    private void EncodePush(IOperand operand)
    {
        switch (operand)
        {
        case Void:
            return;
        case Register r:
            this.LoadRegister(r);
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

    private void LoadLocal(LocalSymbol local)
    {
        var index = this.GetLocalIndex(local);
        if (index is null) return;
        this.InstructionEncoder.LoadLocal(index.Value);
    }

    private void LoadRegister(Register register)
    {
        var index = this.GetRegisterIndex(register);
        if (index is null) return;
        this.InstructionEncoder.LoadLocal(index.Value);
    }

    private void StoreLocal(LocalSymbol local)
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
