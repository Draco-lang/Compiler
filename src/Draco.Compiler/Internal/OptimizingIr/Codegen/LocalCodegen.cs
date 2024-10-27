using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.OptimizingIr.Instructions;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;

/// <summary>
/// Generates IR code on function-local level.
/// </summary>
internal sealed class LocalCodegen : BoundTreeVisitor<IOperand>
{
    private WellKnownTypes WellKnownTypes => this.compilation.WellKnownTypes;

    private readonly Compilation compilation;
    private readonly Procedure procedure;

    private BasicBlock currentBasicBlock;
    private bool isDetached;
    private int blockIndex = 0;

    public LocalCodegen(Compilation compilation, Procedure procedure)
    {
        this.compilation = compilation;
        this.procedure = procedure;
        // NOTE: Attach block takes care of the null
        this.currentBasicBlock = default!;
        this.AttachBlock(procedure.Entry);
    }

    private void Compile(BoundStatement stmt) => stmt.Accept(this);
    private IOperand Compile(BoundExpression expr) => expr.Accept(this);

    private void AttachBlock(BasicBlock basicBlock)
    {
        this.currentBasicBlock = basicBlock;
        this.currentBasicBlock.Index = this.blockIndex++;
        this.isDetached = false;
    }
    private void DetachBlock() => this.isDetached = true;

    /// <summary>
    /// Writes an instruction to the current basic block.
    /// </summary>
    /// <param name="instr">The instruction to write.</param>
    public void Write(IInstruction instr)
    {
        // Happens, when the basic block got detached and there's code left over to compile
        // Example:
        //     goto foo;
        //     y = x;    // This is inaccessible, current BB is null here!
        //     foo:
        //
        // Another simple example would be code after return
        if (this.isDetached && !instr.IsValidInUnreachableContext) return;
        this.currentBasicBlock.InsertLast(instr);
    }

    /// <summary>
    /// Writes the necessary instructions to assign a value to a target.
    /// </summary>
    /// <param name="target">The target to assign to.</param>
    /// <param name="value">The value to assign.</param>
    public void WriteAssignment(VariableSymbol target, IOperand value)
    {
        value = this.BoxIfNeeded(target.Type, value);
        this.Write(Store(target, value));
    }

    /// <summary>
    /// Defines a register in the current procedure.
    /// </summary>
    /// <param name="type">The type of value the register will hold.</param>
    /// <returns>The defined register.</returns>
    public Register DefineRegister(TypeSymbol type) => this.procedure.DefineRegister(type);

    private BasicBlock DefineBasicBlock(LabelSymbol label) => this.procedure.DefineBasicBlock(label);
    private int DefineLocal(LocalSymbol local) => this.procedure.DefineLocal(local);

    private static bool NeedsBoxing(TypeSymbol targetType, TypeSymbol sourceType)
    {
        var targetIsValueType = targetType.Substitution.IsValueType;
        var sourceIsValueType = sourceType.Substitution.IsValueType;
        return !targetIsValueType && sourceIsValueType;
    }

    private static bool NeedsUnboxing(TypeSymbol targetType, TypeSymbol sourceType)
    {
        var targetIsValueType = targetType.Substitution.IsValueType;
        var sourceIsValueType = sourceType.Substitution.IsValueType;
        return targetIsValueType && !sourceIsValueType;
    }

    private IOperand BoxIfNeeded(TypeSymbol targetType, IOperand source)
    {
        if (source.Type is null) throw new System.ArgumentException("source must be a typed operand", nameof(source));

        var needsBox = NeedsBoxing(targetType, source.Type);
        var needsUnbox = NeedsUnboxing(targetType, source.Type);

        if (needsBox)
        {
            var result = this.DefineRegister(targetType.Substitution);
            this.Write(Box(result, targetType.Substitution, source));
            return result;
        }

        if (needsUnbox)
        {
            // TODO
            throw new System.NotImplementedException();
        }

        return source;
    }

    private void PatchLoadTarget(IInstruction loadInstr, Register target)
    {
        switch (loadInstr)
        {
        case LoadInstruction load:
            load.Target = target;
            break;
        case LoadElementInstruction loadElement:
            loadElement.Target = target;
            break;
        case LoadFieldInstruction loadField:
            loadField.Target = target;
            break;
        default:
            throw new System.ArgumentOutOfRangeException(nameof(loadInstr));
        }
    }

    private void PatchStoreSource(IInstruction storeInstr, TypeSymbol targetType, IOperand source)
    {
        source = this.BoxIfNeeded(targetType, source);
        switch (storeInstr)
        {
        case StoreInstruction store:
            store.Source = source;
            break;
        case StoreElementInstruction storeElement:
            storeElement.Source = source;
            break;
        case StoreFieldInstruction storeField:
            storeField.Source = source;
            break;
        default:
            throw new System.ArgumentOutOfRangeException(nameof(storeInstr));
        }
    }

    // Manifesting an expression as an address
    private IOperand CompileToAddress(BoundExpression expression)
    {
        switch (expression)
        {
        case BoundLocalExpression local:
        {
            var target = this.DefineRegister(new ReferenceTypeSymbol(local.TypeRequired));
            this.Write(AddressOf(target, local.Local));
            return target;
        }
        default:
        {
            // We allocate a local so we can take its address
            var local = new SynthetizedLocalSymbol(expression.TypeRequired, false);
            this.procedure.DefineLocal(local);
            // Store the value in it
            var value = this.Compile(expression);
            this.Write(Store(local, value));
            // Take its address
            var target = this.DefineRegister(new ReferenceTypeSymbol(expression.TypeRequired));
            this.Write(AddressOf(target, local));
            return target;
        }
        }
    }

    private IOperand? CompileReceiver(BoundCallExpression call)
    {
        if (call.Receiver is null) return null;
        // Box receiver, if needed
        if (call.Method.ContainingSymbol is TypeSymbol methodContainer
         && NeedsBoxing(methodContainer, call.Receiver.TypeRequired))
        {
            var valueReceiver = this.Compile(call.Receiver);
            var receiver = this.DefineRegister(methodContainer);
            this.Write(Box(receiver, methodContainer, valueReceiver));
            return receiver;
        }
        else
        {
            return call.Receiver.TypeRequired.IsValueType
                ? this.CompileToAddress(call.Receiver)
                : this.Compile(call.Receiver);
        }
    }

    private (IInstruction Load, IInstruction Store) CompileLvalue(BoundLvalue lvalue)
    {
        switch (lvalue)
        {
        case BoundLocalLvalue local:
        {
            return (Load: Load(default!, local.Local), Store: Store(local.Local, default!));
        }
        case BoundFieldLvalue field:
        {
            var receiver = field.Receiver is null ? null : this.Compile(field.Receiver);
            if (receiver is null)
            {
                var src = field.Field;
                return (Load: Load(default!, src), Store: Store(src, default!));
            }
            else
            {
                return (
                    Load: LoadField(default!, receiver, field.Field),
                    Store: StoreField(receiver, field.Field, default!));
            }
        }
        case BoundArrayAccessLvalue arrayAccess:
        {
            var array = this.Compile(arrayAccess.Array);
            var indices = arrayAccess.Indices
                .Select(this.Compile)
                .ToList();
            return (Load: LoadElement(default!, array, indices), Store: StoreElement(array, indices, default!));
        }
        default:
            throw new System.ArgumentOutOfRangeException(nameof(lvalue));
        }
    }

    // Statements //////////////////////////////////////////////////////////////

    public override IOperand VisitSequencePointStatement(BoundSequencePointStatement node)
    {
        // Emit the sequence point
        this.Write(SequencePoint(node.Range));

        // If we need to emit a NOP, emit it
        if (node.EmitNop) this.Write(Nop());

        // Compile the statement, if there is one
        if (node.Statement is not null) this.Compile(node.Statement);

        return default!;
    }

    public override IOperand VisitLabelStatement(BoundLabelStatement node)
    {
        // Define a new basic block
        var newBasicBlock = this.DefineBasicBlock(node.Label);

        // Here we thread the previous basic block to this one
        // Basically an implicit goto
        this.Write(Jump(newBasicBlock));
        this.AttachBlock(newBasicBlock);

        return default!;
    }

    public override IOperand VisitConditionalGotoStatement(BoundConditionalGotoStatement node)
    {
        var condition = this.Compile(node.Condition);

        // In case the condition is a never type, we don't bother writing out the then and else bodies,
        // as they can not be evaluated
        // Note, that for side-effects we still emit the condition code
        if (SymbolEqualityComparer.Default.Equals(node.Condition.TypeRequired, WellKnownTypes.Never)) return default(Void);

        // Allocate blocks
        var thenBlock = this.DefineBasicBlock(node.Target);
        var elseBlock = this.DefineBasicBlock(new SynthetizedLabelSymbol());
        // Branch based on condition
        this.Write(Branch(condition, thenBlock, elseBlock));
        // We fall-through to the else block implicitly
        this.AttachBlock(elseBlock);

        return default!;
    }

    // Expressions /////////////////////////////////////////////////////////////

    public override IOperand VisitStringExpression(BoundStringExpression node) =>
        throw new System.InvalidOperationException("should have been lowered");

    public override IOperand VisitSequencePointExpression(BoundSequencePointExpression node)
    {
        // Emit the sequence point
        this.Write(SequencePoint(node.Range));

        // If we need to emit a NOP, emit it
        if (node.EmitNop) this.Write(Nop());

        // Emit the expression
        return this.Compile(node.Expression);
    }

    public override IOperand VisitCallExpression(BoundCallExpression node)
    {
        var receiver = this.CompileReceiver(node);
        var args = node.Arguments
            .Zip(node.Method.Parameters)
            .Select(pair => this.BoxIfNeeded(pair.Second.Type, this.Compile(pair.First)))
            .ToImmutableArray();

        var proc = node.Method;
        if (proc.Codegen is { } codegen)
        {
            if (receiver is not null)
            {
                throw new System.NotImplementedException();
            }
            return codegen(this, node.TypeRequired, args);
        }
        else
        {
            var callResult = this.DefineRegister(node.TypeRequired);
            this.Write(Call(callResult, proc, receiver, args));
            return callResult;
        }
    }

    public override IOperand VisitObjectCreationExpression(BoundObjectCreationExpression node)
    {
        var ctor = node.Constructor;
        var args = node.Arguments.Select(this.Compile).ToList();
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(NewObject(result, ctor, args));
        return result;
    }

    public override IOperand VisitDelegateCreationExpression(BoundDelegateCreationExpression node)
    {
        var receiver = node.Receiver is null ? null : this.Compile(node.Receiver);
        var function = node.Method;
        var delegateCtor = node.DelegateConstructor;
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(NewDelegate(result, receiver, function, delegateCtor));
        return result;
    }

    public override IOperand VisitArrayAccessExpression(BoundArrayAccessExpression node)
    {
        var array = this.Compile(node.Array);
        var indices = node.Indices.Select(this.Compile).ToList();
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(LoadElement(result, array, indices));
        return result;
    }

    public override IOperand VisitArrayCreationExpression(BoundArrayCreationExpression node)
    {
        var dimensions = node.Sizes.Select(this.Compile).ToList();
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(NewArray(result, node.ElementType, dimensions));
        return result;
    }

    public override IOperand VisitGotoExpression(BoundGotoExpression node)
    {
        var target = this.DefineBasicBlock(node.Target);
        this.Write(Jump(target));
        this.DetachBlock();
        return default(Void);
    }

    public override IOperand VisitBlockExpression(BoundBlockExpression node)
    {
        // Define all locals
        foreach (var local in node.Locals) this.DefineLocal(local);

        // Find locals that we care about for visible scope
        var locals = node.Locals
            .OfType<SourceLocalSymbol>()
            .ToList();

        // Start scope
        if (locals.Count > 0) this.Write(StartScope(locals));

        // Compile all of the statements within
        foreach (var stmt in node.Statements) this.Compile(stmt);
        // Compile value
        var result = this.Compile(node.Value);

        // End scope
        if (locals.Count > 0) this.Write(EndScope());

        return result;
    }

    public override IOperand VisitAssignmentExpression(BoundAssignmentExpression node)
    {
        var right = this.Compile(node.Right);
        var (leftLoad, leftStore) = this.CompileLvalue(node.Left);
        var toStore = right;

        if (node.CompoundOperator is not null)
        {
            var leftValue = this.DefineRegister(node.Left.Type);
            // Patch
            this.PatchLoadTarget(leftLoad, leftValue);
            this.Write(leftLoad);
            if (node.CompoundOperator.Codegen is { } codegen)
            {
                toStore = codegen(this, node.TypeRequired, [leftValue, right]);
            }
            else
            {
                // TODO
                throw new System.NotImplementedException();
            }
        }

        // Patch
        this.PatchStoreSource(leftStore, node.Left.Type, toStore);
        this.Write(leftStore);
        return toStore;
    }

    public override IOperand VisitReturnExpression(BoundReturnExpression node)
    {
        var operand = this.Compile(node.Value);
        operand = this.BoxIfNeeded(this.procedure.ReturnType, operand);
        this.Write(Ret(operand));
        this.DetachBlock();
        return default!;
    }

    public override IOperand VisitLocalExpression(BoundLocalExpression node)
    {
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(Load(result, node.Local));
        return result;
    }

    public override IOperand VisitParameterExpression(BoundParameterExpression node)
    {
        var result = this.DefineRegister(node.TypeRequired);
        this.Write(Load(result, node.Parameter));
        return result;
    }

    public override IOperand VisitLiteralExpression(BoundLiteralExpression node) =>
        new Constant(node.Value, node.TypeRequired);
    public override IOperand VisitUnitExpression(BoundUnitExpression node) => default(Void);

    public override IOperand VisitFieldExpression(BoundFieldExpression node)
    {
        // Check, if constant literal that has to be inlined
        if (node.Field.IsLiteral)
        {
            // NOTE: Literals possibly have a different type than the signature of the global
            var defaultValue = node.Field.LiteralValue;
            if (!BinderFacts.TryGetLiteralType(defaultValue, this.WellKnownTypes, out var literalType))
            {
                throw new System.InvalidOperationException();
            }
            return new Constant(defaultValue, literalType);
        }

        var result = this.DefineRegister(node.TypeRequired);

        if (node.Receiver is null)
        {
            // Regular global
            this.Write(Load(result, node.Field));
            return result;
        }

        // Member access
        var receiver = this.Compile(node.Receiver);
        this.Write(LoadField(result, receiver, node.Field));
        return result;
    }

    // Lowered nodes //////////////////////////////////////////////////////////

    public override IOperand VisitAndExpression(BoundAndExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitOrExpression(BoundOrExpression node) => ShouldHaveBeenLowered(node);

    public override IOperand VisitIfExpression(BoundIfExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitWhileExpression(BoundWhileExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitForExpression(BoundForExpression node) => ShouldHaveBeenLowered(node);

    public override IOperand VisitIndexGetExpression(BoundIndexGetExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitIndexSetExpression(BoundIndexSetExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitPropertyGetExpression(BoundPropertyGetExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitPropertySetExpression(BoundPropertySetExpression node) => ShouldHaveBeenLowered(node);

    public override IOperand VisitIndirectCallExpression(BoundIndirectCallExpression node) => ShouldHaveBeenLowered(node);

    public override IOperand VisitRelationalExpression(BoundRelationalExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitUnaryExpression(BoundUnaryExpression node) => ShouldHaveBeenLowered(node);
    public override IOperand VisitBinaryExpression(BoundBinaryExpression node) => ShouldHaveBeenLowered(node);

    // Illegal nodes //////////////////////////////////////////////////////////

    public override IOperand VisitFunctionGroupExpression(BoundFunctionGroupExpression node) => Illegal(node);
    public override IOperand VisitTypeExpression(BoundTypeExpression node) => Illegal(node);
    public override IOperand VisitModuleExpression(BoundModuleExpression node) => Illegal(node);

    // Error utils ////////////////////////////////////////////////////////////

    private static IOperand ShouldHaveBeenLowered(BoundNode node) =>
        throw new System.InvalidOperationException($"node {node.GetType().Name} should have been lowered");

    private static IOperand Illegal(BoundNode node) =>
        throw new System.InvalidOperationException($"illegal node {node.GetType().Name} in code generation");
}
