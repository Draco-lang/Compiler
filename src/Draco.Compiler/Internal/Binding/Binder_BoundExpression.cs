using System;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
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
        UntypedTypeExpression type => this.TypeTypeExpression(type, constraints, diagnostics),
        UntypedUnitExpression unit => this.TypeUnitExpression(unit, constraints, diagnostics),
        UntypedLiteralExpression literal => this.TypeLiteralExpression(literal, constraints, diagnostics),
        UntypedStringExpression str => this.TypeStringExpression(str, constraints, diagnostics),
        UntypedParameterExpression @param => this.TypeParameterExpression(param, constraints, diagnostics),
        UntypedLocalExpression local => this.TypeLocalExpression(local, constraints, diagnostics),
        UntypedGlobalExpression global => this.TypeGlobalExpression(global, constraints, diagnostics),
        UntypedFieldExpression field => this.TypeFieldExpression(field, constraints, diagnostics),
        UntypedPropertyGetExpression prop => this.TypePropertyGetExpression(prop, constraints, diagnostics),
        UntypedIndexGetExpression index => this.TypeIndexGetExpression(index, constraints, diagnostics),
        UntypedFunctionGroupExpression group => this.TypeFunctionGroupExpression(group, constraints, diagnostics),
        UntypedReferenceErrorExpression err => this.TypeReferenceErrorExpression(err, constraints, diagnostics),
        UntypedReturnExpression @return => this.TypeReturnExpression(@return, constraints, diagnostics),
        UntypedBlockExpression block => this.TypeBlockExpression(block, constraints, diagnostics),
        UntypedGotoExpression @goto => this.TypeGotoExpression(@goto, constraints, diagnostics),
        UntypedIfExpression @if => this.TypeIfExpression(@if, constraints, diagnostics),
        UntypedWhileExpression @while => this.TypeWhileExpression(@while, constraints, diagnostics),
        UntypedCallExpression call => this.TypeCallExpression(call, constraints, diagnostics),
        UntypedIndirectCallExpression call => this.TypeIndirectCallExpression(call, constraints, diagnostics),
        UntypedAssignmentExpression assignment => this.TypeAssignmentExpression(assignment, constraints, diagnostics),
        UntypedUnaryExpression ury => this.TypeUnaryExpression(ury, constraints, diagnostics),
        UntypedBinaryExpression bin => this.TypeBinaryExpression(bin, constraints, diagnostics),
        UntypedRelationalExpression rel => this.TypeRelationalExpression(rel, constraints, diagnostics),
        UntypedAndExpression and => this.TypeAndExpression(and, constraints, diagnostics),
        UntypedOrExpression or => this.TypeOrExpression(or, constraints, diagnostics),
        UntypedMemberExpression mem => this.TypeMemberExpression(mem, constraints, diagnostics),
        UntypedDelayedExpression delay => this.TypeDelayedExpression(delay, constraints, diagnostics),
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

    private BoundUnexpectedExpression TypeTypeExpression(UntypedTypeExpression type, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // A type expression is illegal by itself, report it
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalTypeExpression,
            location: type.Syntax?.Location,
            formatArgs: type.Type.Name));
        return new BoundUnexpectedExpression(type.Syntax);
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

    private BoundExpression TypeFieldExpression(UntypedFieldExpression field, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = field.Reciever is null ? null : this.TypeExpression(field.Reciever, constraints, diagnostics);
        return new BoundFieldExpression(field.Syntax, receiver, field.Field);
    }

    private BoundExpression TypePropertyGetExpression(UntypedPropertyGetExpression prop, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = prop.Receiver is null ? null : this.TypeExpression(prop.Receiver, constraints, diagnostics);
        return new BoundPropertyGetExpression(prop.Syntax, receiver, prop.Getter);
    }

    private BoundExpression TypeIndexGetExpression(UntypedIndexGetExpression index, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = this.TypeExpression(index.Receiver, constraints, diagnostics);
        var indices = index.Indices.Select(x => this.TypeExpression(x, constraints, diagnostics)).ToImmutableArray();
        return new BoundIndexGetExpression(index.Syntax, receiver, index.Getter.Result, indices);
    }

    private BoundExpression TypeFunctionGroupExpression(UntypedFunctionGroupExpression group, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // A function group expression is illegal by itself, report it
        diagnostics.Add(Diagnostic.Create(
            template: SymbolResolutionErrors.IllegalFounctionGroupExpression,
            location: group.Syntax?.Location,
            formatArgs: group.Functions.First().Name));
        return new BoundUnexpectedExpression(group.Syntax);
    }

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
        var receiver = call.Receiver is null ? null : this.TypeExpression(call.Receiver, constraints, diagnostics);
        var function = call.Method.Result;
        var typedArgs = call.Arguments
            .Select(arg => this.TypeExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        return new BoundCallExpression(call.Syntax, receiver, function, typedArgs);
    }

    private BoundExpression TypeIndirectCallExpression(UntypedIndirectCallExpression call, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var function = this.TypeExpression(call.Method, constraints, diagnostics);
        var typedArgs = call.Arguments
            .Select(arg => this.TypeExpression(arg, constraints, diagnostics))
            .ToImmutableArray();
        var resultType = constraints.Unwrap(call.TypeRequired);
        return new BoundIndirectCallExpression(call.Syntax, function, typedArgs, resultType);
    }

    private BoundExpression TypeAssignmentExpression(UntypedAssignmentExpression assignment, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var typedRight = this.TypeExpression(assignment.Right, constraints, diagnostics);
        var compoundOperator = assignment.CompoundOperator?.Result;

        // NOTE: This is how we deal with properties and indexers
        if (assignment.Left is UntypedPropertySetLvalue prop)
        {
            if (prop.Setter.IsError)
            {
                return new BoundReferenceErrorExpression(prop.Syntax, prop.Setter);
            }
            return new BoundPropertySetExpression(
                assignment.Syntax,
                prop.Receiver is null ? null : this.TypeExpression(prop.Receiver, constraints, diagnostics),
                prop.Setter,
                compoundOperator is not null
                    ? this.CompoundPropertyExpression(
                        assignment.Syntax,
                        prop.Receiver is null ? null : this.TypeExpression(prop.Receiver, constraints, diagnostics),
                        typedRight,
                        ((IPropertyAccessorSymbol)prop.Setter).Property,
                        compoundOperator,
                        ImmutableArray<BoundExpression>.Empty,
                        diagnostics)
                    : typedRight);
        }

        if (assignment.Left is UntypedIndexSetLvalue index)
        {
            if (index.Setter.Result.IsError)
            {
                return new BoundReferenceErrorExpression(index.Syntax, index.Setter.Result);
            }
            return new BoundIndexSetExpression(
                assignment.Syntax,
                this.TypeExpression(index.Receiver, constraints, diagnostics),
                index.Setter.Result,
                compoundOperator is not null
                    ? this.CompoundPropertyExpression(assignment.Syntax,
                        this.TypeExpression(index.Receiver, constraints, diagnostics),
                        typedRight,
                        ((IPropertyAccessorSymbol)index.Setter.Result).Property,
                        compoundOperator,
                        index.Indices.Select(x => this.TypeExpression(x, constraints, diagnostics)).ToImmutableArray(),
                        diagnostics)
                    : typedRight,
                index.Indices.Select(x => this.TypeExpression(x, constraints, diagnostics)).ToImmutableArray());
        }

        else if (assignment.Left is UntypedMemberLvalue mem && mem.Member.Result[0] is PropertySymbol pr)
        {
            var setter = pr.Setter;
            if (setter is null)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: SymbolResolutionErrors.CannotSetGetOnlyProperty,
                    location: assignment.Syntax?.Location,
                    pr.FullName));
                setter = new NoOverloadFunctionSymbol(1);
            }
            return new BoundPropertySetExpression(
                assignment.Syntax,
                this.TypeExpression(mem.Accessed, constraints, diagnostics),
                setter,
                compoundOperator is not null
                    ? this.CompoundPropertyExpression(
                        assignment.Syntax,
                        this.TypeExpression(mem.Accessed, constraints, diagnostics),
                        typedRight,
                        pr,
                        compoundOperator,
                        ImmutableArray<BoundExpression>.Empty,
                        diagnostics)
                    : typedRight);
        }
        var typedLeft = this.TypeLvalue(assignment.Left, constraints, diagnostics);
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
        if (members.Length == 1 && members[0] is ITypedSymbol member)
        {
            if (member is FieldSymbol field) return new BoundFieldExpression(mem.Syntax, left, field);
            if (member is PropertySymbol prop)
            {
                var getter = prop.Getter;
                if (getter is null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        template: SymbolResolutionErrors.CannotGetSetOnlyProperty,
                        location: mem.Syntax?.Location,
                        prop.FullName));
                    getter = new NoOverloadFunctionSymbol(0);
                }
                return new BoundPropertyGetExpression(mem.Syntax, left, getter);
            }
            return new BoundMemberExpression(mem.Syntax, left, (Symbol)member, member.Type);
        }
        else
        {
            // NOTE: This can happen in case of function with more overloads, but without () after the function name. For example builder.Append
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.IllegalFounctionGroupExpression,
                location: mem.Syntax?.Location,
                formatArgs: members[0].Name));
            return new BoundUnexpectedExpression(mem.Syntax);
        }
    }

    private BoundExpression TypeDelayedExpression(UntypedDelayedExpression delay, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Just take result and type that
        var result = delay.Promise.Result;
        return this.TypeExpression(result, constraints, diagnostics);
    }

    private BoundExpression CompoundPropertyExpression(Api.Syntax.SyntaxNode? syntax, BoundExpression? receiver, BoundExpression right, PropertySymbol prop, FunctionSymbol compoundOperator, ImmutableArray<BoundExpression> args, DiagnosticBag diagnostics)
    {
        var getter = prop.Getter;
        if (getter is null)
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.CannotGetSetOnlyProperty,
                location: syntax?.Location,
                prop.FullName));
            getter = new NoOverloadFunctionSymbol(args.Length);
        }
        var getterCall = new BoundCallExpression(null, receiver, getter, args);
        return new BoundBinaryExpression(syntax, compoundOperator, getterCall, right, right.TypeRequired);
    }
}
