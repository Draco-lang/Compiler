using System.Collections.Generic;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Utilities for constructing instructions.
/// </summary>
internal static class InstructionFactory
{
    public static NopInstruction Nop() => new();
    public static BoxInstruction Box(Register target, TypeSymbol boxedType, IOperand value) =>
        new(target, boxedType, value);
    public static StoreInstruction Store(IOperand target, IOperand source) => new(target, source);
    public static StoreElementInstruction StoreElement(IOperand array, IEnumerable<IOperand> indices, IOperand source) =>
        new(array, indices, source);
    public static StoreFieldInstruction StoreField(IOperand receiver, FieldSymbol field, IOperand source) =>
        new(receiver, field, source);
    public static LoadInstruction Load(Register target, IOperand source) => new(target, source);
    public static LoadElementInstruction LoadElement(Register target, IOperand array, IEnumerable<IOperand> indices) =>
        new(target, array, indices);
    public static LoadFieldInstruction LoadField(Register target, IOperand receiver, FieldSymbol field) =>
        new(target, receiver, field);
    public static RetInstruction Ret(IOperand value) => new(value);
    public static JumpInstruction Jump(BasicBlock target) => new(target);
    public static BranchInstruction Branch(IOperand condition, BasicBlock then, BasicBlock @else) =>
        new(condition, then, @else);
    public static CallInstruction Call(Register target, FunctionSymbol proc, IEnumerable<IOperand> args) =>
        new(target, proc, args);
    public static MemberCallInstruction MemberCall(Register target, FunctionSymbol proc, IOperand receiver, IEnumerable<IOperand> args) =>
        new(target, proc, receiver, args);
    public static NewObjectInstruction NewObject(Register target, FunctionSymbol ctor, IEnumerable<IOperand> args) =>
        new(target, ctor, args);
    public static NewArrayInstruction NewArray(Register target, TypeSymbol elementType, IEnumerable<IOperand> dimensions) =>
        new(target, elementType, dimensions);
    public static ArrayLengthInstruction ArrayLength(Register target, IOperand array) =>
        new(target, array);
    public static ArithmeticInstruction Arithmetic(Register target, ArithmeticOp op, IOperand left, IOperand right) =>
        new(target, op, left, right);
    public static ArithmeticInstruction Add(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Add, left, right);
    public static ArithmeticInstruction Sub(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Sub, left, right);
    public static ArithmeticInstruction Mul(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Mul, left, right);
    public static ArithmeticInstruction Div(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Div, left, right);
    public static ArithmeticInstruction Rem(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Rem, left, right);
    public static ArithmeticInstruction Less(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Less, left, right);
    public static ArithmeticInstruction Equal(Register target, IOperand left, IOperand right) =>
        Arithmetic(target, ArithmeticOp.Equal, left, right);
    public static SequencePoint SequencePoint(SyntaxRange? range) => new(range);
    public static StartScope StartScope(IEnumerable<LocalSymbol> locals) => new(locals);
    public static EndScope EndScope() => new();
}
