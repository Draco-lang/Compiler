using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Lowering;

/// <summary>
/// Performs local rewrites of the source code.
/// </summary>
internal partial class LocalRewriter(Compilation compilation) : BoundTreeRewriter
{
    /// <summary>
    /// Represents a value that was temporarily stored.
    /// </summary>
    /// <param name="Symbol">The synthetized local symbol.</param>
    /// <param name="Reference">The expression referencing the stored temporary.</param>
    /// <param name="Assignment">The assignment that stores the temporary.</param>
    private readonly record struct TemporaryStorage(
        LocalSymbol? Symbol,
        BoundExpression Reference,
        BoundStatement Assignment);

    private WellKnownTypes WellKnownTypes => compilation.WellKnownTypes;

    private BoundLiteralExpression LiteralExpression(object? value)
    {
        if (!BinderFacts.TryGetLiteralType(value, this.WellKnownTypes, out var literalType))
        {
            throw new ArgumentOutOfRangeException(nameof(value));
        }
        return BoundTreeFactory.LiteralExpression(value, literalType);
    }

    public override BoundNode VisitUnaryExpression(BoundUnaryExpression node)
    {
        // Unary operators simply get turned into calls, as they use practically the same logic
        //
        // @ x
        //
        // =>
        //
        // @(x)

        var arg = (BoundExpression)node.Operand.Accept(this);
        return CallExpression(
            receiver: null,
            method: node.Operator,
            arguments: [arg]);
    }

    public override BoundNode VisitBinaryExpression(BoundBinaryExpression node)
    {
        // Binary operators simply get turned into calls, as they use practically the same logic
        //
        // x @ y
        //
        // =>
        //
        // @(x, y)

        var left = (BoundExpression)node.Left.Accept(this);
        var right = (BoundExpression)node.Right.Accept(this);
        return CallExpression(
            receiver: null,
            method: node.Operator,
            arguments: [left, right]);
    }

    public override BoundNode VisitCallExpression(BoundCallExpression node)
    {
        if (!node.Method.IsVariadic) return base.VisitCallExpression(node);

        // For variadics we desugar to array construction
        //
        // method(fixedArg1, ..., fixedArgN, varArg1, ..., varArgM);
        //
        // =>
        //
        // {
        //     // We pre-eval all args to keep evaluation order
        //     val tmp1 = fixedArg1;
        //     ...
        //     val tmpN = fixedArgN;
        //     val varArgs = Array(M);
        //     varArgs[0] = varArg1;
        //     ...
        //     varArgs[M] = varArgM;
        //     method(tmp1, ..., tmpN, varArgs);
        // }

        var receiver = (BoundExpression?)node.Receiver?.Accept(this);

        var method = node.Method;
        var fixedArgs = node.Arguments
            .Take(method.Parameters.Length - 1)
            .Select(n => n.Accept(this))
            .Cast<BoundExpression>()
            .Select(this.StoreTemporary)
            .ToList();
        var variadicType = method.Parameters[^1].Type;

        if (!BinderFacts.TryGetVariadicElementType(variadicType, out var elementType))
        {
            // NOTE: Should not happen
            throw new InvalidOperationException();
        }

        var varArgs = new SynthetizedLocalSymbol(variadicType, false);
        var varArgCount = node.Arguments.Length - (method.Parameters.Length - 1);
        var varArgAssignments = node.Arguments
            .Skip(method.Parameters.Length - 1)
            .Select(n => n.Accept(this))
            .Cast<BoundExpression>()
            .Select((n, i) => ExpressionStatement(AssignmentExpression(
                compoundOperator: null,
                left: ArrayAccessLvalue(
                    array: LocalExpression(varArgs),
                    indices: [this.LiteralExpression(i)]),
                right: n)) as BoundStatement);

        return BlockExpression(
            locals: fixedArgs
                .Select(a => a.Symbol)
                .OfType<LocalSymbol>()
                .Append(varArgs)
                .ToImmutableArray(),
            statements: fixedArgs
                .Select(a => a.Assignment)
                .Append(ExpressionStatement(AssignmentExpression(
                    compoundOperator: null,
                    left: LocalLvalue(varArgs),
                    right: ArrayCreationExpression(
                        elementType: elementType,
                        sizes: [this.LiteralExpression(varArgCount)],
                        type: this.WellKnownTypes.InstantiateArray(elementType)))))
                .Concat(varArgAssignments)
                .ToImmutableArray(),
            value: CallExpression(
                receiver: receiver,
                method: method,
                arguments: fixedArgs
                    .Select(a => a.Reference)
                    .Append(LocalExpression(varArgs))
                    .ToImmutableArray()));
    }

    public override BoundNode VisitIndirectCallExpression(BoundIndirectCallExpression node)
    {
        // We desugar delegate calls into calling the invoke method
        var methodType = node.Method.TypeRequired;
        if (!methodType.IsDelegateType) return base.VisitIndirectCallExpression(node);

        // func(a, b, c)
        //
        // =>
        //
        // func.Invoke(a, b, c)

        var method = (BoundExpression)node.Method.Accept(this);
        var args = node.Arguments
            .Select(n => n.Accept(this))
            .Cast<BoundExpression>()
            .ToImmutableArray();

        var invokeMethod = method.TypeRequired.InvokeMethod;
        Debug.Assert(invokeMethod is not null);

        return CallExpression(
            receiver: method,
            method: invokeMethod,
            arguments: args);
    }

    public override BoundNode VisitBlockExpression(BoundBlockExpression node)
    {
        // We only keep useful statements
        var statements = node.Statements
            .Select(s => s.Accept(this))
            .Cast<BoundStatement>()
            .Where(s => !IsUseless(s))
            .ToImmutableArray();
        var value = (BoundExpression)node.Value.Accept(this);

        // If the node became empty, we can erase it
        if (node.Locals.Length == 0 && statements.Length == 0 && IsUseless(value))
        {
            // Useless block, erase it
            return BoundUnitExpression.Default;
        }
        else
        {
            // Just update it
            return node.Update(node.Locals, statements, value);
        }
    }

    public override BoundNode VisitIfExpression(BoundIfExpression node)
    {
        // if (condition) then_expr else else_expr
        //
        // =>
        //
        // {
        //     var result;
        //     if (condition) goto then_label;
        //     goto else_label;
        //     then_label:
        //         result = then_expr;
        //         goto finally_label;
        //     else_label:
        //         result = else_expr;
        //     finally_label:
        //     @sequence point
        //     result
        // }
        //
        // NOTE: We are putting a sequence point after the finally label to erase
        // the previous one, so after evaluating the 'then' block and jumping to
        // 'finally', the debugger won't highlight the 'else' block by accident,
        // because the last sequence point was there

        var condition = (BoundExpression)node.Condition.Accept(this);
        var then = (BoundExpression)node.Then.Accept(this);
        var @else = (BoundExpression)node.Else.Accept(this);

        var result = new SynthetizedLocalSymbol(node.TypeRequired, true);
        var thenLabel = new SynthetizedLabelSymbol("then");
        var elseLabel = new SynthetizedLabelSymbol("else");
        var finallyLabel = new SynthetizedLabelSymbol("finally");

        return BlockExpression(
            locals: [result],
            statements:
            [
                LocalDeclaration(result, null),
                ConditionalGotoStatement(condition, thenLabel),
                ExpressionStatement(GotoExpression(elseLabel)),
                LabelStatement(thenLabel),
                ExpressionStatement(AssignmentExpression(null, LocalLvalue(result), then)),
                ExpressionStatement(GotoExpression(finallyLabel)),
                LabelStatement(elseLabel),
                ExpressionStatement(AssignmentExpression(null, LocalLvalue(result), @else)),
                LabelStatement(finallyLabel),
                SequencePointStatement(
                    statement: null,
                    range: null,
                    emitNop: true),
            ],
            value: LocalExpression(result));
    }

    public override BoundNode VisitWhileExpression(BoundWhileExpression node)
    {
        // while (condition)
        // {
        //     body...
        // }
        //
        // =>
        //
        // continue_label:
        //     if (!condition) goto break_label;
        //     body...
        //     goto continue_label;
        // break_label:

        var condition = (BoundExpression)node.Condition.Accept(this);
        var body = (BoundExpression)node.Then.Accept(this);

        var result = BlockExpression(
            locals: [],
            statements:
            [
                LabelStatement(node.ContinueLabel),
                ConditionalGotoStatement(
                    condition: UnaryExpression(
                        @operator: this.WellKnownTypes.Bool_Not,
                        operand: condition),
                    target: node.BreakLabel),
                ExpressionStatement(body),
                ExpressionStatement(GotoExpression(node.ContinueLabel)),
                LabelStatement(node.BreakLabel),
            ],
            value: BoundUnitExpression.Default);
        // Blocks can be desugared too, pass through
        return result.Accept(this);
    }

    public override BoundNode VisitForExpression(BoundForExpression node)
    {
        // TODO: Once we have try-finally and a way to do checked casts, fix this
        //  1. Wrap in try-finally
        //  2. Call Dispose in finally block, if enumerator is IDisposable

        // for (i in sequence)
        // {
        //     body...
        // }
        //
        // =>
        //
        // {
        //     val enumerator = sequence.GetEnumerator();
        //     while (enumerator.MoveNext()) {
        //         i = enumerator.Current;
        //         body...
        //     }
        // }
        //
        // NOTE: For loops are desugared into while loops,
        // which are then desugared again using the existing lowering step
        // Because of this, we do not need to lower each individual member here, they will be lowered
        // while rewriting the while loop

        var enumerator = new SynthetizedLocalSymbol(node.GetEnumeratorMethod.ReturnType, false);

        // NOTE: Checked during binding
        var currentProp = (PropertySymbol)node.CurrentProperty;
        if (currentProp.Getter is null) throw new InvalidOperationException();

        var result = BlockExpression(
            locals: [enumerator],
            statements:
            [
                ExpressionStatement(AssignmentExpression(
                            compoundOperator: null,
                            left: LocalLvalue(enumerator),
                            right: CallExpression(
                                receiver: node.Sequence,
                                method: node.GetEnumeratorMethod,
                                arguments: []))),
                ExpressionStatement(WhileExpression(
                    condition: CallExpression(
                        receiver: LocalExpression(enumerator),
                        method: node.MoveNextMethod,
                        arguments: []),
                    then: BlockExpression(
                        locals: [node.Iterator],
                        statements:
                        [
                            ExpressionStatement(AssignmentExpression(
                                                    compoundOperator: null,
                                                    left: LocalLvalue(node.Iterator),
                                                    right: PropertyGetExpression(
                                                        receiver: LocalExpression(enumerator),
                                                        getter: currentProp.Getter))),
                            ExpressionStatement(node.Then),
                        ],
                        value: BoundUnitExpression.Default),
                    continueLabel: node.ContinueLabel,
                    breakLabel: node.BreakLabel)),
            ],
            value: BoundUnitExpression.Default);
        // Desugaring the while-loop
        return result.Accept(this);
    }

    public override BoundNode VisitRelationalExpression(BoundRelationalExpression node)
    {
        // In case there are only two operands, don't do any of the optimizations below
        if (node.Comparisons.Length == 1)
        {
            var left = (BoundExpression)node.First.Accept(this);
            var right = (BoundExpression)node.Comparisons[0].Next.Accept(this);
            var result = BinaryExpression(
                left: left,
                @operator: node.Comparisons[0].Operator,
                right: right);
            return result.Accept(this);
        }

        // expr1 < expr2 == expr3 > expr4 != ...
        //
        // =>
        //
        // {
        //     val tmp1 = expr1;
        //     val tmp2 = expr2;
        //     val tmp3 = expr3;
        //     val tmp4 = expr4;
        //     ...
        //     tmp1 < tmp2 && tmp2 == tmp3 && tmp3 > tmp4 && tmp4 != ...
        // }

        // Store all expressions as temporary variables
        var tmpVariables = new List<TemporaryStorage>
        {
            this.StoreTemporary(node.First)
        };
        foreach (var item in node.Comparisons) tmpVariables.Add(this.StoreTemporary(item.Next));

        // Build pairs of comparisons from symbol references
        var comparisons = new List<BoundExpression>();
        for (var i = 0; i < node.Comparisons.Length; ++i)
        {
            var left = tmpVariables[i].Reference;
            var op = node.Comparisons[i].Operator;
            var right = tmpVariables[i + 1].Reference;
            comparisons.Add(BinaryExpression(
                left: left,
                @operator: op,
                right: right));
        }

        // Fold them into conjunctions
        var conjunction = comparisons.Aggregate((result, current) => AndExpression(result, current));
        // Desugar them, conjunctions can be desugared too
        conjunction = (BoundExpression)conjunction.Accept(this);

        // Wrap up in block
        return BlockExpression(
            locals: tmpVariables
                .Select(tmp => tmp.Symbol)
                .OfType<LocalSymbol>()
                .ToImmutableArray(),
            statements: tmpVariables
                .Select(t => t.Assignment)
                .ToImmutableArray(),
            value: conjunction);
    }

    public override BoundNode VisitAndExpression(BoundAndExpression node)
    {
        // expr1 and expr2
        //
        // =>
        //
        // if (expr1) expr2 else false

        var left = (BoundExpression)node.Left.Accept(this);
        var right = (BoundExpression)node.Right.Accept(this);

        var result = IfExpression(
            condition: left,
            then: right,
            @else: this.LiteralExpression(false),
            type: this.WellKnownTypes.SystemBoolean);
        // If-expressions can be lowered too
        return result.Accept(this);
    }

    public override BoundNode VisitOrExpression(BoundOrExpression node)
    {
        // expr1 or expr2
        //
        // =>
        //
        // if (expr1) true else expr2

        var left = (BoundExpression)node.Left.Accept(this);
        var right = (BoundExpression)node.Right.Accept(this);

        var result = IfExpression(
            condition: left,
            then: this.LiteralExpression(true),
            @else: right,
            type: this.WellKnownTypes.SystemBoolean);
        // If-expressions can be lowered too
        return result.Accept(this);
    }

    public override BoundNode VisitStringExpression(BoundStringExpression node)
    {
        // Empty string
        if (node.Parts.Length == 0) return this.LiteralExpression(string.Empty);
        // A single string
        if (node.Parts.Length == 1 && node.Parts[0] is BoundStringText singleText) return this.LiteralExpression(singleText.Text);
        // A single interpolated part
        if (node.Parts.Length == 1 && node.Parts[0] is BoundStringInterpolation singleInterpolation)
        {
            // Lower the expression
            var arg = (BoundExpression)singleInterpolation.Value.Accept(this);
            return CallExpression(
                receiver: arg,
                method: this.WellKnownTypes.SystemObject_ToString,
                arguments: []);
        }
        // We need to desugar into string.Format("format string", array of args)
        // Build up interpolation string and lower interpolated expressions
        var formatString = new StringBuilder();
        var args = new List<BoundExpression>();
        foreach (var part in node.Parts)
        {
            if (part is BoundStringText text)
            {
                formatString.Append(text.Text);
            }
            else if (part is BoundStringInterpolation interpolation)
            {
                formatString
                    .Append('{')
                    .Append(args.Count)
                    .Append('}');
                // Lower the expression
                var arg = (BoundExpression)interpolation.Value.Accept(this);
                args.Add(arg);
            }
        }

        var arrayType = this.WellKnownTypes.ArrayType.GenericInstantiate(this.WellKnownTypes.SystemObject);
        var arrayLocal = new SynthetizedLocalSymbol(arrayType, true);

        var arrayAssignmentBuilder = ImmutableArray.CreateBuilder<BoundStatement>(1 + args.Count);

        // var args = new object[number of interpolated expressions];
        arrayAssignmentBuilder.Add(LocalDeclaration(
            local: arrayLocal,
            value: ArrayCreationExpression(
                elementType: this.WellKnownTypes.SystemObject,
                sizes: [this.LiteralExpression(args.Count)],
                type: this.WellKnownTypes.InstantiateArray(this.WellKnownTypes.SystemObject))));

        for (var i = 0; i < args.Count; i++)
        {
            // args[i] = interpolatedExpr;
            arrayAssignmentBuilder.Add(ExpressionStatement(AssignmentExpression(
                compoundOperator: null,
                left: ArrayAccessLvalue(
                    array: LocalExpression(arrayLocal),
                    indices: [this.LiteralExpression(i)]),
                right: args[i])));
        }

        // {
        //     var args = new object[...];
        //     args[0] = ...;
        //     args[1] = ...;
        //     string.Format("...", args);
        // }
        var result = BlockExpression(
            locals: [arrayLocal],
            statements: arrayAssignmentBuilder.ToImmutable(),
            value: CallExpression(
                method: this.WellKnownTypes.SystemString_Format,
                receiver: null,
                arguments: [this.LiteralExpression(formatString.ToString()), LocalExpression(arrayLocal)]));

        return result.Accept(this);
    }

    public override BoundNode VisitPropertySetExpression(BoundPropertySetExpression node)
    {
        // property = expr
        //
        // =>
        //
        // {
        //     var tmp = expr;
        //     property_set(tmp);
        //     tmp
        // }

        var receiver = node.Receiver is null ? null : (BoundExpression)node.Receiver.Accept(this);
        var setter = node.Setter;
        var value = (BoundExpression)node.Value.Accept(this);

        var tmp = this.StoreTemporary(value);

        var result = BlockExpression(
            locals: tmp.Symbol is null
                ? []
                : [tmp.Symbol],
            statements:
            [
                tmp.Assignment,
                ExpressionStatement(CallExpression(
                    receiver: receiver,
                    method: setter,
                    arguments: [tmp.Reference])),
            ],
            value: tmp.Reference);

        return result.Accept(this);
    }

    public override BoundNode VisitPropertyGetExpression(BoundPropertyGetExpression node)
    {
        // property
        //
        // =>
        //
        // property_get()

        var receiver = node.Receiver is null ? null : (BoundExpression)node.Receiver.Accept(this);
        var getter = node.Getter;

        return CallExpression(
            receiver: receiver,
            method: getter,
            arguments: []);
    }

    public override BoundNode VisitIndexSetExpression(BoundIndexSetExpression node)
    {
        // indexed[idx1, idx2, ...] = expr
        //
        // =>
        //
        // {
        //     // Pre-evaluate the indices to keep evaluation order
        //     var tmp1 = idx1;
        //     var tmp2 = idx2;
        //     ...
        //     var tmpN = expr;
        //     indexed.Item_set(tmp1, tmp2, ..., tmpN);
        //     tmpN
        // }

        var receiver = (BoundExpression)node.Receiver.Accept(this);
        var setter = node.Setter;
        var args = node.Indices
            .Append(node.Value)
            .Select(x => (BoundExpression)x.Accept(this))
            .ToImmutableArray();

        var argsStored = args
            .Select(this.StoreTemporary)
            .ToList();

        return BlockExpression(
            locals: argsStored
                .Select(a => a.Symbol)
                .OfType<LocalSymbol>()
                .ToImmutableArray(),
            statements: argsStored
                .Select(a => a.Assignment)
                .Append(ExpressionStatement(CallExpression(
                    receiver: receiver,
                    method: setter,
                    arguments: argsStored
                        .Select(a => a.Reference)
                        .ToImmutableArray())))
                .ToImmutableArray(),
            value: argsStored[^1].Reference);
    }

    public override BoundNode VisitIndexGetExpression(BoundIndexGetExpression node)
    {
        // indexed[x]
        //
        // =>
        //
        // indexed.Item_get(x)

        var receiver = (BoundExpression)node.Receiver.Accept(this);
        var getter = node.Getter;
        var args = node.Indices
            .Select(x => (BoundExpression)x.Accept(this))
            .ToImmutableArray();

        return CallExpression(
            receiver: receiver,
            method: getter,
            arguments: args);
    }

    // Utility to store an expression to a temporary variable
    private TemporaryStorage StoreTemporary(BoundExpression expr)
    {
        // Optimization: if it's already a symbol reference, leave as-is
        // Optimization: if it's a literal, don't bother copying
        if (expr is BoundLocalExpression
                 or BoundGlobalExpression
                 or BoundParameterExpression
                 or BoundFunctionGroupExpression
                 or BoundLiteralExpression)
        {
            return new(null, expr, BoundNoOpStatement.Default);
        }

        // Otherwise compute and store
        var symbol = new SynthetizedLocalSymbol(expr.TypeRequired, false);
        var symbolRef = LocalExpression(symbol);
        var assignment = LocalDeclaration(
            local: symbol,
            value: (BoundExpression)expr.Accept(this));
        return new(symbol, symbolRef, assignment);
    }

    private static bool IsUseless(BoundStatement stmt) => stmt switch
    {
        BoundExpressionStatement s => IsUseless(s.Expression),
        BoundNoOpStatement => true,
        _ => false,
    };

    private static bool IsUseless(BoundExpression expr) => expr switch
    {
        BoundBlockExpression block => block.Locals.Length == 0
                                   && block.Statements.All(IsUseless)
                                   && IsUseless(block.Value),
        BoundUnitExpression => true,
        _ => false,
    };
}
