using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given untyped expression to a bound expression.
    /// </summary>
    /// <param name="expression">The untyped expression to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The bound expression for <paramref name="expression"/>.</returns>
    internal virtual BoundExpression TypeExpression(UntypedExpression expression, ConstraintSolver constraints, DiagnosticBag diagnostics) => expression switch
    {
        UntypedUnexpectedExpression unexpected => new BoundUnexpectedExpression(unexpected.Syntax),
        UntypedModuleExpression module => this.TypeModuleExpression(module, constraints, diagnostics),
        UntypedUnitExpression unit => this.TypeUnitExpression(unit, constraints, diagnostics),
        UntypedLiteralExpression literal => this.TypeLiteralExpression(literal, constraints, diagnostics),
        UntypedStringExpression str => this.TypeStringExpression(str, constraints, diagnostics),
        UntypedParameterExpression @param => this.TypeParameterExpression(param, constraints, diagnostics),
        UntypedLocalExpression local => this.TypeLocalExpression(local, constraints, diagnostics),
        UntypedGlobalExpression global => this.TypeGlobalExpression(global, constraints, diagnostics),
        UntypedReferenceErrorExpression err => this.TypeReferenceErrorExpression(err, constraints, diagnostics),
        UntypedReturnExpression @return => this.TypeReturnExpression(@return, constraints, diagnostics),
        UntypedBlockExpression block => this.TypeBlockExpression(block, constraints, diagnostics),
        UntypedGotoExpression @goto => this.TypeGotoExpression(@goto, constraints, diagnostics),
        UntypedIfExpression @if => this.TypeIfExpression(@if, constraints, diagnostics),
        UntypedWhileExpression @while => this.TypeWhileExpression(@while, constraints, diagnostics),
        UntypedCallExpression call => this.TypeCallExpression(call, constraints, diagnostics),
        UntypedAssignmentExpression assignment => this.TypeAssignmentExpression(assignment, constraints, diagnostics),
        UntypedUnaryExpression ury => this.TypeUnaryExpression(ury, constraints, diagnostics),
        UntypedBinaryExpression bin => this.TypeBinaryExpression(bin, constraints, diagnostics),
        UntypedRelationalExpression rel => this.TypeRelationalExpression(rel, constraints, diagnostics),
        UntypedAndExpression and => this.TypeAndExpression(and, constraints, diagnostics),
        UntypedOrExpression or => this.TypeOrExpression(or, constraints, diagnostics),
        UntypedMemberExpression mem => this.TypeMemberExpression(mem, constraints, diagnostics),
        UntypedFunctionExpression func => this.TypeFunctionExpression(func, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(expression)),
    };

    private BoundUnexpectedExpression TypeModuleExpression(UntypedModuleExpression module, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // A module expression is illegal by itself, report it
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalModuleExpression,
            location: module.Syntax?.Location,
            formatArgs: module.Module.Name));
        return new BoundUnexpectedExpression(module.Syntax);
    }

    private BoundExpression TypeUnitExpression(UntypedUnitExpression unit, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        unit.Syntax is null ? BoundUnitExpression.Default : new BoundUnitExpression(unit.Syntax);

    private BoundExpression TypeLiteralExpression(UntypedLiteralExpression literal, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundLiteralExpression(literal.Syntax, literal.Value);

    private BoundExpression TypeStringExpression(UntypedStringExpression str, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundStringExpression(str.Syntax, str.Parts.Select(p => this.TypeStringPart(p, constraints, diagnostics)).ToImmutableArray());

    private BoundStringPart TypeStringPart(UntypedStringPart part, ConstraintSolver constraints, DiagnosticBag diagnostics) => part switch
    {
        UntypedUnexpectedStringPart unexpected => new BoundUnexpectedStringPart(unexpected.Syntax),
        UntypedStringText text => new BoundStringText(text.Syntax, text.Text),
        UntypedStringInterpolation interpolation => new BoundStringInterpolation(
            interpolation.Syntax,
            this.TypeExpression(interpolation.Value, constraints, diagnostics)),
        _ => throw new ArgumentOutOfRangeException(nameof(part)),
    };

    private BoundExpression TypeParameterExpression(UntypedParameterExpression param, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundParameterExpression(param.Syntax, param.Parameter);

    private BoundExpression TypeLocalExpression(UntypedLocalExpression local, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundLocalExpression(local.Syntax, constraints.GetTypedLocal(local.Local, diagnostics));

    private BoundExpression TypeGlobalExpression(UntypedGlobalExpression global, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundGlobalExpression(global.Syntax, global.Global);

    private BoundExpression TypeReferenceErrorExpression(UntypedReferenceErrorExpression err, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundReferenceErrorExpression(err.Syntax, err.Symbol);

    private BoundExpression TypeReturnExpression(UntypedReturnExpression @return, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedValue = this.TypeExpression(@return.Value, constraints, diagnostics);
        return new BoundReturnExpression(@return.Syntax, typedValue);
    }

    private BoundExpression TypeBlockExpression(UntypedBlockExpression block, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var locals = block.Locals
            .Select(l => constraints.GetTypedLocal(l, diagnostics))
            .ToImmutableArray();
        var typedStatements = block.Statements
            .Select(s => this.TypeStatement(s, constraints, diagnostics))
            .ToImmutableArray();
        var typedValue = this.TypeExpression(block.Value, constraints, diagnostics);
        return new BoundBlockExpression(block.Syntax, locals, typedStatements, typedValue);
    }

    private BoundExpression TypeGotoExpression(UntypedGotoExpression @goto, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new BoundGotoExpression(@goto.Syntax, @goto.Target);

    private BoundExpression TypeIfExpression(UntypedIfExpression @if, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedCondition = this.TypeExpression(@if.Condition, constraints, diagnostics);
        var typedThen = this.TypeExpression(@if.Then, constraints, diagnostics);
        var typedElse = this.TypeExpression(@if.Else, constraints, diagnostics);
        var resultType = constraints.Unwrap(@if.TypeRequired);
        return new BoundIfExpression(@if.Syntax, typedCondition, typedThen, typedElse, resultType);
    }

    private BoundExpression TypeWhileExpression(UntypedWhileExpression @while, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedCondition = this.TypeExpression(@while.Condition, constraints, diagnostics);
        var typedThen = this.TypeExpression(@while.Then, constraints, diagnostics);
        return new BoundWhileExpression(@while.Syntax, typedCondition, typedThen, @while.ContinueLabel, @while.BreakLabel);
    }

    private BoundExpression TypeCallExpression(UntypedCallExpression call, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // We have 3 cases:
        //  - called is a member access -> direct call with receiver
        //  - called is a function symbol -> direct call without receiver
        //  - anything else -> indirect call

        var receiver = call.Receiver is null ? null : this.TypeExpression(call.Receiver, constraints, diagnostics);
        var typedFunction = this.TypeExpression(call.Method, constraints, diagnostics);
        var typedArgs = call.Arguments
            .Select(arg => this.TypeExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        if (typedFunction is BoundFunctionExpression funcExpr)
        {
            // Direct call
            return new BoundCallExpression(call.Syntax, receiver, funcExpr.Function, typedArgs);
        }
        else
        {
            if (receiver is not null)
            {
                // Indirect call to member
                // TODO
                throw new NotImplementedException();
            }
            else
            {
                // Indirect free-function call
                return new BoundIndirectCallExpression(call.Syntax, typedFunction, typedArgs);
            }
        }
    }

    private BoundExpression TypeAssignmentExpression(UntypedAssignmentExpression assignment, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedLeft = this.TypeLvalue(assignment.Left, constraints, diagnostics);
        var typedRight = this.TypeExpression(assignment.Right, constraints, diagnostics);
        var compoundOperator = assignment.CompoundOperator?.Result;
        return new BoundAssignmentExpression(assignment.Syntax, compoundOperator, typedLeft, typedRight);
    }

    private BoundExpression TypeUnaryExpression(UntypedUnaryExpression ury, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedOperand = this.TypeExpression(ury.Operand, constraints, diagnostics);
        var unaryOperator = ury.Operator.Result;
        var resultType = constraints.Unwrap(ury.TypeRequired);
        return new BoundUnaryExpression(ury.Syntax, unaryOperator, typedOperand, resultType);
    }

    private BoundExpression TypeBinaryExpression(UntypedBinaryExpression bin, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedLeft = this.TypeExpression(bin.Left, constraints, diagnostics);
        var typedRight = this.TypeExpression(bin.Right, constraints, diagnostics);
        var binaryOperator = bin.Operator.Result;
        var resultType = constraints.Unwrap(bin.TypeRequired);
        return new BoundBinaryExpression(bin.Syntax, binaryOperator, typedLeft, typedRight, resultType);
    }

    private BoundExpression TypeRelationalExpression(UntypedRelationalExpression rel, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var first = this.TypeExpression(rel.First, constraints, diagnostics);
        var comparisons = rel.Comparisons
            .Select(cmp => this.TypeComparison(cmp, constraints, diagnostics))
            .ToImmutableArray();
        return new BoundRelationalExpression(rel.Syntax, first, comparisons);
    }

    private BoundComparison TypeComparison(UntypedComparison cmp, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var next = this.TypeExpression(cmp.Next, constraints, diagnostics);
        var comparisonOperator = cmp.Operator.Result;
        return new BoundComparison(cmp.Syntax, comparisonOperator, next);
    }

    private BoundExpression TypeAndExpression(UntypedAndExpression and, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.TypeExpression(and.Left, constraints, diagnostics);
        var right = this.TypeExpression(and.Right, constraints, diagnostics);
        return new BoundAndExpression(and.Syntax, left, right);
    }

    private BoundExpression TypeOrExpression(UntypedOrExpression or, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.TypeExpression(or.Left, constraints, diagnostics);
        var right = this.TypeExpression(or.Right, constraints, diagnostics);
        return new BoundOrExpression(or.Syntax, left, right);
    }

    private BoundExpression TypeMemberExpression(UntypedMemberExpression mem, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.TypeExpression(mem.Accessed, constraints, diagnostics);
        var members = mem.Member.Result;
        // TODO
        throw new NotImplementedException();
        // var resultType = constraints.Unwrap(mem.TypeRequired);

        // return new BoundMemberExpression(mem.Syntax, left, member, resultType);
    }

    private BoundExpression TypeFunctionExpression(UntypedFunctionExpression func, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var funcSymbol = func.Function.Result;
        return new BoundFunctionExpression(func.Syntax, funcSymbol);
    }
}
