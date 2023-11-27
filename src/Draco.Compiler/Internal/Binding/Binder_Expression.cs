using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Solver.Tasks;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    /// <summary>
    /// Binds the given syntax node to an untyped expression.
    /// </summary>
    /// <param name="syntax">The syntax to bind.</param>
    /// <param name="constraints">The constraints that has been collected during the binding process.</param>
    /// <param name="diagnostics">The diagnostics produced during the process.</param>
    /// <returns>The untyped expression for <paramref name="syntax"/>.</returns>
    protected virtual BindingTask<BoundExpression> BindExpression(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => FromResult(new BoundUnexpectedExpression(syntax)),
        GroupingExpressionSyntax grp => this.BindExpression(grp.Expression, constraints, diagnostics),
        StatementExpressionSyntax stmt => this.BindStatementExpression(stmt, constraints, diagnostics),
        LiteralExpressionSyntax lit => this.BindLiteralExpression(lit, constraints, diagnostics),
        StringExpressionSyntax str => this.BindStringExpression(str, constraints, diagnostics),
        NameExpressionSyntax name => this.BindNameExpression(name, constraints, diagnostics),
        BlockExpressionSyntax block => this.BindBlockExpression(block, constraints, diagnostics),
        GotoExpressionSyntax @goto => this.BindGotoExpression(@goto, constraints, diagnostics),
        ReturnExpressionSyntax @return => this.BindReturnExpression(@return, constraints, diagnostics),
        IfExpressionSyntax @if => this.BindIfExpression(@if, constraints, diagnostics),
        WhileExpressionSyntax @while => this.BindWhileExpression(@while, constraints, diagnostics),
        ForExpressionSyntax @for => this.BindForExpression(@for, constraints, diagnostics),
        CallExpressionSyntax call => this.BindCallExpression(call, constraints, diagnostics),
        UnaryExpressionSyntax ury => this.BindUnaryExpression(ury, constraints, diagnostics),
        BinaryExpressionSyntax bin => this.BindBinaryExpression(bin, constraints, diagnostics),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel, constraints, diagnostics),
        MemberExpressionSyntax maccess => this.BindMemberExpression(maccess, constraints, diagnostics),
        GenericExpressionSyntax gen => this.BindGenericExpression(gen, constraints, diagnostics),
        IndexExpressionSyntax index => this.BindIndexExpression(index, constraints, diagnostics),
        _ => throw new ArgumentOutOfRangeException(nameof(syntax)),
    };

    private static BindingTask<BoundExpression> FromResult(BoundExpression expr) => BindingTask.FromResult(expr);

    private async BindingTask<BoundExpression> BindStatementExpression(StatementExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // We just desugar stmt; into { stmt; }
        var stmtTask = this.BindStatement(syntax.Statement, constraints, diagnostics);
        return new BoundBlockExpression(
            syntax: syntax,
            locals: ImmutableArray<LocalSymbol>.Empty,
            statements: ImmutableArray.Create(await stmtTask),
            value: BoundUnitExpression.Default);
    }

    private BindingTask<BoundExpression> BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (!BinderFacts.TryGetLiteralType(syntax.Literal.Value, this.IntrinsicSymbols, out var literalType))
        {
            throw new InvalidOperationException("can not determine literal type");
        }
        return FromResult(new BoundLiteralExpression(syntax, syntax.Literal.Value, literalType));
    }

    private async BindingTask<BoundExpression> BindStringExpression(StringExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var lastNewline = true;
        var cutoff = SyntaxFacts.ComputeCutoff(syntax);
        var partsTask = new List<BindingTask<BoundStringPart>>();
        foreach (var part in syntax.Parts)
        {
            switch (part)
            {
            case TextStringPartSyntax content:
            {
                var text = content.Content.ValueText
                        ?? throw new InvalidOperationException();
                // Apply cutoff
                if (lastNewline && text.StartsWith(cutoff)) text = text[cutoff.Length..];
                partsTask.Add(BindingTask.FromResult<BoundStringPart>(new BoundStringText(syntax, text)));
                lastNewline = content.Content.Kind == TokenKind.StringNewline;
                break;
            }
            case InterpolationStringPartSyntax interpolation:
            {
                partsTask.Add(BindingTask.FromResult<BoundStringPart>(new BoundStringInterpolation(
                    syntax,
                    await this.BindExpression(interpolation.Expression, constraints, diagnostics))));
                lastNewline = false;
                break;
            }
            case UnexpectedStringPartSyntax unexpected:
            {
                partsTask.Add(BindingTask.FromResult<BoundStringPart>(new BoundUnexpectedStringPart(syntax)));
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
        return new BoundStringExpression(syntax, await BindingTask.WhenAll(partsTask), this.IntrinsicSymbols.String);
    }

    private BindingTask<BoundExpression> BindNameExpression(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = BinderFacts.SyntaxMustNotReferenceTypes(syntax)
            ? this.LookupNonTypeValueSymbol(syntax.Name.Text, syntax, diagnostics)
            : this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return FromResult(this.SymbolToExpression(syntax, symbol, constraints, diagnostics));
    }

    private async BindingTask<BoundExpression> BindBlockExpression(BlockExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<LocalSymbol>()
            .ToImmutableArray();
        var statementsTask = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics))
            .ToList();
        var valueTask = syntax.Value is null
            ? FromResult(BoundUnitExpression.Default)
            : binder.BindExpression(syntax.Value, constraints, diagnostics);
        return new BoundBlockExpression(
            syntax,
            locals,
            await BindingTask.WhenAll(statementsTask),
            await valueTask);
    }

    private BindingTask<BoundExpression> BindGotoExpression(GotoExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var target = (LabelSymbol)this.BindLabel(syntax.Target, constraints, diagnostics);
        return FromResult(new BoundGotoExpression(syntax, target));
    }

    private async BindingTask<BoundExpression> BindReturnExpression(ReturnExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var valueTask = syntax.Value is null
            ? FromResult(BoundUnitExpression.Default)
            : this.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax, valueTask, constraints, diagnostics);

        return new BoundReturnExpression(syntax, await valueTask);
    }

    private async BindingTask<BoundExpression> BindIfExpression(IfExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var conditionTask = this.BindExpression(syntax.Condition, constraints, diagnostics);

        // Condition must be bool
        _ = constraints.SameType(
            this.IntrinsicSymbols.Bool,
            conditionTask.GetResultType(syntax.Condition, constraints, diagnostics),
            syntax);

        var thenTask = this.BindExpression(syntax.Then, constraints, diagnostics);
        var elseTask = syntax.Else is null
            ? FromResult(BoundUnitExpression.Default)
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        // Then and else must be compatible types
        var resultType = constraints.AllocateTypeVariable();
        var thenType = thenTask.GetResultType(ExtractValueSyntax(syntax.Then), constraints, diagnostics);
        var elseType = elseTask.GetResultType(ExtractValueSyntax(syntax.Else?.Expression), constraints, diagnostics);
        _ = constraints.CommonType(
            resultType,
            ImmutableArray.Create(thenType, elseType),
            // The location will point at the else value, assuming that the latter expression is
            // the offending one
            // If there is no else clause, we just point at the then clause
            ConstraintLocator.Syntax(syntax.Else is null
                ? ExtractValueSyntax(syntax.Then)
                : ExtractValueSyntax(syntax.Else.Expression))
                .WithRelatedInformation(
                    format: "the other branch is inferred to be {0}",
                    formatArgs: thenType,
                    // If there is an else clause, we annotate the then clause as related info
                    location: ExtractValueSyntax(syntax.Then).Location));

        return new BoundIfExpression(syntax, await conditionTask, await thenTask, await elseTask, resultType);
    }

    private async BindingTask<BoundExpression> BindWhileExpression(WhileExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);

        var conditionTask = binder.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        _ = constraints.SameType(
            this.IntrinsicSymbols.Bool,
            conditionTask.GetResultType(syntax.Condition, constraints, diagnostics),
            syntax);

        var thenTask = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        _ = constraints.SameType(
            IntrinsicSymbols.Unit,
            thenTask.GetResultType(ExtractValueSyntax(syntax.Then), constraints, diagnostics),
            ExtractValueSyntax(syntax.Then));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        return new BoundWhileExpression(syntax, await conditionTask, await thenTask, continueLabel, breakLabel);
    }

    private async BindingTask<BoundExpression> BindForExpression(ForExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);

        // Resolve iterator
        var iterator = binder.DeclaredSymbols
            .OfType<LocalSymbol>()
            .Single();

        this.BindSyntaxToSymbol(syntax.Iterator, iterator);

        var type = syntax.ElementType is null ? null : this.BindTypeToTypeSymbol(syntax.ElementType.Type, diagnostics);
        constraints.DeclareLocal(iterator);
        if (type is not null) ConstraintSolver.UnifyAsserted(iterator.Type, type);

        var sequenceTask = binder.BindExpression(syntax.Sequence, constraints, diagnostics);

        var thenTask = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        _ = constraints.SameType(
            IntrinsicSymbols.Unit,
            thenTask.GetResultType(ExtractValueSyntax(syntax.Then), constraints, diagnostics),
            ExtractValueSyntax(syntax.Then));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        // GetEnumerator
        var getEnumeratorMembersTask = constraints.Member(
            sequenceTask.GetResultType(syntax.Sequence, constraints, diagnostics),
            "GetEnumerator",
            out _,
            syntax.Sequence);

        var getEnumeratorMembers = await getEnumeratorMembersTask;
        if (getEnumeratorMembers.IsError)
        {
            ConstraintSolver.UnifyAsserted(iterator.Type, IntrinsicSymbols.ErrorType);
            return new BoundForExpression(
                syntax,
                iterator,
                await sequenceTask,
                await thenTask,
                continueLabel,
                breakLabel,
                new NoOverloadFunctionSymbol(0),
                new NoOverloadFunctionSymbol(0),
                UndefinedMemberSymbol.Instance);
        }

        // Look up the overload
        var getEnumeratorFunctions = GetFunctions(getEnumeratorMembers);
        var getEnumeratorTask = constraints.Overload(
            "GetEnumerator",
            getEnumeratorFunctions,
            ImmutableArray<ConstraintSolver.Argument>.Empty,
            out var enumeratorType,
            syntax.Sequence);

        // Look up MoveNext
        var moveNextMembersTask = constraints.Member(
            enumeratorType,
            "MoveNext",
            out _,
            syntax.Sequence);

        var moveNextMembers = await moveNextMembersTask;
        SolverTask<FunctionSymbol> moveNextTask;
        // Don't propagate errors
        if (moveNextMembers.IsError)
        {
            moveNextTask = SolverTask.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(0));
        }
        else
        {
            var moveNextFunctions = GetFunctions(moveNextMembers);
            moveNextTask = constraints.Overload(
                "MoveNext",
                moveNextFunctions,
                ImmutableArray<ConstraintSolver.Argument>.Empty,
                out var moveNextReturnType,
                syntax.Sequence);
            // MoveNext should return bool
            _ = constraints.SameType(
                this.IntrinsicSymbols.Bool,
                moveNextReturnType,
                syntax.Sequence);
        }

        // Look up Current
        var currentTask = constraints.Member(
            enumeratorType,
            "Current",
            out var currentType,
            syntax.Sequence);

        // Element type of the Enumerator must be assignable to the iterator type of the for loop
        _ = constraints.Assignable(
            iterator.Type,
            currentType,
            syntax.ElementType as SyntaxNode ?? syntax.Iterator);

        // Current needs to be a gettable property
        var current = await currentTask;

        // Don't propagate error
        if (!current.IsError && (current is not PropertySymbol propSymbol || propSymbol.Getter is null))
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.NotGettableProperty,
                location: syntax.Sequence.Location,
                formatArgs: current.Name));
        }

        return new BoundForExpression(
            syntax,
            iterator,
            await sequenceTask,
            await thenTask,
            continueLabel,
            breakLabel,
            await getEnumeratorTask,
            await moveNextTask,
            current);
    }

    private async BindingTask<BoundExpression> BindCallExpression(CallExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var methodTask = this.BindExpression(syntax.Function, constraints, diagnostics);
        var argsTask = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToList();

        var method = await methodTask;

        var argsForConstraints = argsTask
            .Zip(syntax.ArgumentList.Values)
            .Select(pair => constraints.Arg(pair.Second, pair.First, diagnostics))
            .ToImmutableArray();
        if (method is BoundFunctionGroupExpression group)
        {
            // Simple overload
            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                group.Functions[0].Name,
                group.Functions,
                argsForConstraints,
                out var _,
                syntax.Function);

            return new BoundCallExpression(syntax, group.Receiver, await symbolPromise, await BindingTask.WhenAll(argsTask));
        }
        else
        {
            var callPromise = constraints.Call(
                method.TypeRequired,
                argsForConstraints,
                out var resultType,
                syntax);
            return new BoundIndirectCallExpression(syntax, method, await BindingTask.WhenAll(argsTask), resultType);
        }
    }

    private async BindingTask<BoundExpression> BindUnaryExpression(UnaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Get the unary operator symbol
        var operatorName = FunctionSymbol.GetUnaryOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var operandTask = this.BindExpression(syntax.Operand, constraints, diagnostics);

        // Resolve symbol overload
        var symbolPromise = constraints.Overload(
            operatorName,
            GetFunctions(operatorSymbol),
            ImmutableArray.Create(constraints.Arg(syntax.Operand, operandTask, diagnostics)),
            out _,
            syntax.Operator);

        return new BoundUnaryExpression(syntax, await symbolPromise, await operandTask);
    }

    private async BindingTask<BoundExpression> BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var leftTask = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Right must be assignable to left
            _ = constraints.Assignable(
                leftTask.GetResultType(syntax.Left, constraints, diagnostics),
                rightTask.GetResultType(syntax.Right, constraints, diagnostics),
                syntax);

            var left = await leftTask;
            if (left is BoundPropertySetLvalue propertySet)
            {
                return new BoundPropertySetExpression(
                    syntax,
                    propertySet.Receiver,
                    propertySet.Setter,
                    await rightTask);
            }
            else if (left is BoundIndexSetLvalue indexSet)
            {
                return new BoundIndexSetExpression(
                    syntax,
                    indexSet.Receiver,
                    indexSet.Setter,
                    indexSet.Indices,
                    await rightTask);
            }
            else
            {
                return new BoundAssignmentExpression(syntax, null, left, await rightTask);
            }
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var leftTask = this.BindExpression(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Both left and right must be bool
            _ = constraints.SameType(
                this.IntrinsicSymbols.Bool,
                leftTask.GetResultType(syntax.Left, constraints, diagnostics),
                syntax.Left);
            _ = constraints.SameType(
                this.IntrinsicSymbols.Bool,
                rightTask.GetResultType(syntax.Left, constraints, diagnostics),
                syntax.Right);

            return syntax.Operator.Kind == TokenKind.KeywordAnd
                ? new BoundAndExpression(syntax, await leftTask, await rightTask)
                : new BoundOrExpression(syntax, await leftTask, await rightTask);
        }
        else if (SyntaxFacts.TryGetOperatorOfCompoundAssignment(syntax.Operator.Kind, out var nonCompound))
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(nonCompound);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);

            var leftTask = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                operatorName,
                GetFunctions(operatorSymbol),
                ImmutableArray.Create(
                    constraints.Arg(syntax.Left, leftTask, diagnostics),
                    constraints.Arg(syntax.Right, rightTask, diagnostics)),
                out var resultType,
                syntax.Operator);
            // The result of the binary operator must be assignable to the left-hand side
            // For example, a + b in the form of a += b means that a + b has to result in a type
            // that is assignable to a, hence the extra constraint
            _ = constraints.Assignable(
                leftTask.GetResultType(syntax.Left, constraints, diagnostics),
                resultType,
                syntax);

            var left = await leftTask;
            if (left is BoundPropertySetLvalue propertySet)
            {
                var property = ((IPropertyAccessorSymbol)propertySet.Setter).Property;
                var getter = GetGetterSymbol(syntax, property, diagnostics);

                return new BoundPropertySetExpression(
                    syntax,
                    propertySet.Receiver,
                    propertySet.Setter,
                    new BoundBinaryExpression(
                        syntax,
                        await symbolPromise,
                        new BoundPropertyGetExpression(
                            syntax,
                            propertySet.Receiver,
                            getter),
                        await rightTask));
            }
            else if (left is BoundIndexSetLvalue indexSet)
            {
                var property = ((IPropertyAccessorSymbol)indexSet.Setter).Property;
                var getter = GetGetterSymbol(syntax, property, diagnostics);

                return new BoundIndexSetExpression(
                    syntax,
                    indexSet.Receiver,
                    indexSet.Setter,
                    indexSet.Indices,
                    new BoundBinaryExpression(
                        syntax,
                        await symbolPromise,
                        new BoundIndexGetExpression(
                            syntax,
                            indexSet.Receiver,
                            getter,
                            indexSet.Indices),
                        await rightTask));
            }
            else
            {
                return new BoundAssignmentExpression(syntax, await symbolPromise, await leftTask, await rightTask);
            }
        }
        else
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(syntax.Operator.Kind);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
            var leftTask = this.BindExpression(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                operatorName,
                GetFunctions(operatorSymbol),
                ImmutableArray.Create(
                    constraints.Arg(syntax.Left, leftTask, diagnostics),
                    constraints.Arg(syntax.Right, rightTask, diagnostics)),
                out _,
                syntax.Operator);

            return new BoundBinaryExpression(syntax, await symbolPromise, await leftTask, await rightTask);
        }
    }

    private async BindingTask<BoundExpression> BindRelationalExpression(RelationalExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var first = this.BindExpression(syntax.Left, constraints, diagnostics);
        var comparisons = new List<BindingTask<BoundComparison>>();
        var prev = first;
        foreach (var comparisonSyntax in syntax.Comparisons)
        {
            var next = this.BindExpression(comparisonSyntax.Right, constraints, diagnostics);
            var comparison = this.BindComparison(prev, next, comparisonSyntax, constraints, diagnostics);
            prev = next;
            comparisons.Add(comparison);
        }
        return new BoundRelationalExpression(
            syntax,
            await first,
            await BindingTask.WhenAll(comparisons),
            this.IntrinsicSymbols.Bool);
    }

    private async BindingTask<BoundComparison> BindComparison(
        BindingTask<BoundExpression> left,
        BindingTask<BoundExpression> right,
        ComparisonElementSyntax syntax,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        // Get the comparison operator symbol
        var operatorName = FunctionSymbol.GetComparisonOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);

        // NOTE: We know it must be bool, no need to pass it on to comparison
        // Resolve symbol overload
        var symbolPromise = constraints.Overload(
            operatorName,
            GetFunctions(operatorSymbol),
            ImmutableArray.Create(
                constraints.Arg(syntax, left, diagnostics),
                constraints.Arg(syntax, right, diagnostics)),
            out var resultType,
            syntax.Operator);
        // For safety, we assume it has to be bool
        _ = constraints.SameType(this.IntrinsicSymbols.Bool, resultType, syntax.Operator);

        return new BoundComparison(syntax, await symbolPromise, await right);
    }

    private async BindingTask<BoundExpression> BindMemberExpression(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiverTask = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;
        var receiver = await receiverTask;
        if (receiver is BoundReferenceErrorExpression err)
        {
            // Error, don't cascade
            return new BoundReferenceErrorExpression(syntax, err.Symbol);
        }

        var container = ExtractContainer(receiver);
        if (container is not null)
        {
            Func<Symbol, bool> pred = BinderFacts.SyntaxMustNotReferenceTypes(syntax)
                ? BinderFacts.IsNonTypeValueSymbol
                : BinderFacts.IsValueSymbol;

            var members = container.StaticMembers
                .Where(m => m.Name == memberName && m.Visibility != Api.Semantics.Visibility.Private)
                .Where(pred)
                .ToImmutableArray();

            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToExpression(syntax, symbol, constraints, diagnostics);
        }
        else
        {
            // Value, add constraint
            var memberTask = constraints.Member(receiver.TypeRequired, memberName, out var memberType, syntax);
            var member = await memberTask;

            switch (member)
            {
            case Symbol when member.IsError:
                return new BoundReferenceErrorExpression(syntax, member);
            case FunctionSymbol func:
                return new BoundFunctionGroupExpression(syntax, receiver, ImmutableArray.Create(func));
            case OverloadSymbol overload:
                return new BoundFunctionGroupExpression(syntax, receiver, overload.Functions);
            case FieldSymbol field:
                return new BoundFieldExpression(syntax, receiver, field);
            case PropertySymbol prop:
                // It could be array length
                if (prop.GenericDefinition is ArrayLengthPropertySymbol)
                {
                    return new BoundArrayLengthExpression(syntax, receiver);
                }
                else
                {
                    var getter = GetGetterSymbol(syntax, prop, diagnostics);
                    return new BoundPropertyGetExpression(syntax, receiver, getter);
                }
            default:
                // TODO
                throw new NotImplementedException();
            }
        }
    }

    private async BindingTask<BoundExpression> BindIndexExpression(IndexExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiverTask = this.BindExpression(syntax.Indexed, constraints, diagnostics);
        var argsTask = syntax.IndexList.Values
            .Select(x => this.BindExpression(x, constraints, diagnostics))
            .ToImmutableArray();

        var receiver = await receiverTask;
        if (receiver is BoundReferenceErrorExpression err)
        {
            return new BoundReferenceErrorExpression(syntax, err.Symbol);
        }

        var receiverType = receiverTask.GetResultType(syntax.Indexed, constraints, diagnostics);
        receiverType = await ConstraintSolver.Substituted(receiverType);

        SolverTask<FunctionSymbol> accessorTask;

        // General indexer
        var indexers = receiverType
            .Members
            .OfType<PropertySymbol>()
            .Where(x => x.IsIndexer)
            .Select(x => x.Getter)
            .OfType<FunctionSymbol>()
            .ToImmutableArray();
        if (indexers.Length == 0)
        {
            diagnostics.Add(Diagnostic.Create(
                template: SymbolResolutionErrors.NoGettableIndexerInType,
                location: syntax.Location,
                formatArgs: receiver.Type));
            accessorTask = SolverTask.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(argsTask.Length));
        }
        else
        {
            accessorTask = constraints.Overload(
                "operator[]",
                indexers,
                argsTask
                    .Zip(syntax.IndexList.Values)
                    .Select(pair => constraints.Arg(pair.Second, pair.First, diagnostics))
                    .ToImmutableArray(),
                out _,
                syntax);
        }

        var accessor = await accessorTask;
        var arrayIndexProperty = (accessor.GenericDefinition as IPropertyAccessorSymbol)?.Property as ArrayIndexPropertySymbol;
        if (arrayIndexProperty is not null)
        {
            // Array getter
            return new BoundArrayAccessExpression(syntax, receiver, await BindingTask.WhenAll(argsTask));
        }
        else
        {
            return new BoundIndexGetExpression(syntax, receiver, await accessorTask, await BindingTask.WhenAll(argsTask));
        }
    }

    private async BindingTask<BoundExpression> BindGenericExpression(GenericExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var instantiatedTask = this.BindExpression(syntax.Instantiated, constraints, diagnostics);
        var args = syntax.Arguments.Values
            .Select(arg => this.BindTypeToTypeSymbol(arg, diagnostics))
            .ToImmutableArray();

        var instantiated = await instantiatedTask;
        if (instantiated is BoundFunctionGroupExpression group)
        {
            // Filter for same number of generic parameters
            var withSameNoParams = group.Functions
                .Where(f => f.GenericParameters.Length == args.Length)
                .ToImmutableArray();
            if (withSameNoParams.Length == 0)
            {
                // No generic functions with this number of parameters
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.NoGenericFunctionWithParamCount,
                    location: syntax.Location,
                    formatArgs: new object[] { group.Functions[0].Name, args.Length }));

                // Return a sentinel
                // NOTE: Is this the right one to return?
                return new BoundReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
            }
            else
            {
                // There are functions with this same number of parameters
                // Instantiate each possibility
                var instantiatedFuncs = withSameNoParams
                    .Select(f => f.GenericInstantiate(f.ContainingSymbol, args))
                    .ToImmutableArray();

                // Wrap them back up in a function group
                return new BoundFunctionGroupExpression(syntax, group.Receiver, instantiatedFuncs);
            }
        }
        else if (instantiated is BoundTypeExpression type)
        {
            if (type.Type.GenericParameters.Length != args.Length)
            {
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.GenericTypeParamCountMismatch,
                    location: syntax.Location,
                    formatArgs: new object[] { type.Type.Name, args.Length }));

                // Return a sentinel
                // NOTE: Is this the right one to return?
                return new BoundReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
            }
            return new BoundTypeExpression(syntax, type.Type.GenericInstantiate(type.Type.ContainingSymbol, args));
        }
        else
        {
            // Tried to instantiate something that can not be instantiated
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.NotGenericConstruct,
                location: syntax.Location));

            // Return a sentinel
            // NOTE: Is this the right one to return?
            return new BoundReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
        }
    }

    private BoundExpression SymbolToExpression(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (symbol.IsError) return new BoundReferenceErrorExpression(syntax, symbol);
        switch (symbol)
        {
        case Symbol when symbol.IsError:
            return new BoundReferenceErrorExpression(syntax, symbol);
        case ModuleSymbol module:
            // NOTE: Hack, see the note above this method definition
            this.BindSyntaxToSymbol(syntax, module);
            return new BoundModuleExpression(syntax, module);
        case TypeSymbol type:
            // NOTE: Hack, see the note above this method definition
            this.BindTypeSyntaxToSymbol(syntax, type);
            return new BoundTypeExpression(syntax, type);
        case ParameterSymbol param:
            return new BoundParameterExpression(syntax, param);
        case LocalSymbol local:
            return new BoundLocalExpression(syntax, local);
        case GlobalSymbol global:
            return new BoundGlobalExpression(syntax, global);
        case PropertySymbol prop:
            var getter = GetGetterSymbol(syntax, prop, diagnostics);
            return new BoundPropertyGetExpression(syntax, null, getter);
        case FunctionSymbol func:
            return new BoundFunctionGroupExpression(syntax, null, ImmutableArray.Create(func));
        case OverloadSymbol overload:
            return new BoundFunctionGroupExpression(syntax, null, overload.Functions);
        default:
            throw new InvalidOperationException();
        }
    }

    private static Symbol? ExtractContainer(BoundExpression expression) => expression switch
    {
        BoundModuleExpression m => m.Module,
        BoundTypeExpression t => t.Type,
        _ => null,
    };

    private static ImmutableArray<FunctionSymbol> GetFunctions(Symbol symbol) => symbol switch
    {
        FunctionSymbol f => ImmutableArray.Create(f),
        OverloadSymbol o => o.Functions,
        _ => ImmutableArray<FunctionSymbol>.Empty,
    };

    [return: NotNullIfNotNull(nameof(syntax))]
    private static ExpressionSyntax? ExtractValueSyntax(ExpressionSyntax? syntax) => syntax switch
    {
        null => null,
        IfExpressionSyntax @if => ExtractValueSyntax(@if.Then),
        WhileExpressionSyntax @while => ExtractValueSyntax(@while.Then),
        ForExpressionSyntax @for => ExtractValueSyntax(@for.Then),
        BlockExpressionSyntax block => block.Value is null ? block : ExtractValueSyntax(block.Value),
        _ => syntax,
    };
}
