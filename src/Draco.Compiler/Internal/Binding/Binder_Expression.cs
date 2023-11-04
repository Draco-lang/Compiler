using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding.Tasks;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

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

    private BindingTask<BoundExpression> BindStringExpression(StringExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var lastNewline = true;
        var cutoff = SyntaxFacts.ComputeCutoff(syntax);
        var parts = ImmutableArray.CreateBuilder<UntypedStringPart>();
        foreach (var part in syntax.Parts)
        {
            switch (part)
            {
            case TextStringPartSyntax content:
            {
                var text = content.Content.ValueText
                        ?? throw new InvalidOperationException();
                // Single line string or string newline or malformed input
                if (!lastNewline || !text.StartsWith(cutoff)) parts.Add(new UntypedStringText(syntax, text));
                else parts.Add(new UntypedStringText(syntax, text[cutoff.Length..]));
                lastNewline = content.Content.Kind == TokenKind.StringNewline;
                break;
            }
            case InterpolationStringPartSyntax interpolation:
            {
                parts.Add(new UntypedStringInterpolation(
                    syntax,
                    this.BindExpression(interpolation.Expression, constraints, diagnostics)));
                lastNewline = false;
                break;
            }
            case UnexpectedStringPartSyntax unexpected:
            {
                parts.Add(new UntypedUnexpectedStringPart(syntax));
                break;
            }
            default:
                throw new ArgumentOutOfRangeException();
            }
        }
        return new BoundStringExpression(syntax, parts.ToImmutable(), this.IntrinsicSymbols.String);
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindNameExpression(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = BinderFacts.SyntaxMustNotReferenceTypes(syntax)
            ? this.LookupNonTypeValueSymbol(syntax.Name.Text, syntax, diagnostics)
            : this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return FromResult(this.SymbolToExpression(syntax, symbol, constraints, diagnostics));
    }

    private BindingTask<BoundExpression> BindBlockExpression(BlockExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics))
            .ToImmutableArray();
        var value = syntax.Value is null
            ? BoundUnitExpression.Default
            : binder.BindExpression(syntax.Value, constraints, diagnostics);
        return new BoundBlockExpression(syntax, locals, statements, value);
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindGotoExpression(GotoExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var target = (LabelSymbol)this.BindLabel(syntax.Target, constraints, diagnostics);
        return FromResult(new BoundGotoExpression(syntax, target));
    }

    private BindingTask<BoundExpression> BindReturnExpression(ReturnExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var value = syntax.Value is null
            ? BoundUnitExpression.Default
            : this.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax, value, constraints);

        return new BoundReturnExpression(syntax, value);
#else
        throw new NotImplementedException();
#endif
    }

    private async BindingTask<BoundExpression> BindIfExpression(IfExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var conditionTask = this.BindExpression(syntax.Condition, constraints, diagnostics);

        // Condition must be bool
        _ = constraints.SameType(
            this.IntrinsicSymbols.Bool,
            conditionTask.GetResultTypeRequired(constraints),
            syntax);

        var thenTask = this.BindExpression(syntax.Then, constraints, diagnostics);
        var elseTask = syntax.Else is null
            ? FromResult(BoundUnitExpression.Default)
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        // Then and else must be compatible types
        var resultType = constraints.AllocateTypeVariable();
        _ = constraints.CommonType(
            resultType,
            ImmutableArray.Create(
                thenTask.GetResultTypeRequired(constraints),
                elseTask.GetResultTypeRequired(constraints)),
            // The location will point at the else value, assuming that the latter expression is
            // the offending one
            // If there is no else clause, we just point at the then clause
            ConstraintLocator.Syntax(syntax.Else is null
                ? ExtractValueSyntax(syntax.Then)
                : ExtractValueSyntax(syntax.Else.Expression))
                .WithRelatedInformation(
                    format: "the other branch is inferred to be {0}",
                    formatArgs: thenTask.GetResultTypeRequired(constraints),
                    // If there is an else clause, we annotate the then clause as related info
                    location: ExtractValueSyntax(syntax.Then).Location));

        return new BoundIfExpression(syntax, await conditionTask, await thenTask, await elseTask, resultType);
    }

    private BindingTask<BoundExpression> BindWhileExpression(WhileExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var binder = this.GetBinder(syntax);

        var condition = binder.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        constraints.SameType(this.IntrinsicSymbols.Bool, condition.TypeRequired, syntax);

        var then = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        constraints.SameType(IntrinsicSymbols.Unit, then.TypeRequired, ExtractValueSyntax(syntax.Then));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        return new BoundWhileExpression(syntax, condition, then, continueLabel, breakLabel);
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindForExpression(ForExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var binder = this.GetBinder(syntax);

        // Resolve iterator
        var iterator = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .Single();

        var type = syntax.ElementType is null ? null : this.BindTypeToTypeSymbol(syntax.ElementType.Type, diagnostics);
        var elementType = constraints.DeclareLocal(iterator, type);

        var sequence = binder.BindExpression(syntax.Sequence, constraints, diagnostics);

        var then = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        constraints.SameType(IntrinsicSymbols.Unit, then.TypeRequired, ExtractValueSyntax(syntax.Then));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        // GetEnumerator
        var getEnumeratorMethodsPromise = constraints.Member(sequence.TypeRequired, "GetEnumerator", out _, syntax.Sequence);

        var exprPromise = constraints.Await(getEnumeratorMethodsPromise, BoundExpression () =>
        {
            var getEnumeratorResult = getEnumeratorMethodsPromise.Result;
            if (getEnumeratorResult.IsError)
            {
                ConstraintSolver.UnifyAsserted(elementType, IntrinsicSymbols.ErrorType);
                return new BoundForExpression(
                    syntax,
                    iterator,
                    sequence,
                    then,
                    continueLabel,
                    breakLabel,
                    ConstraintPromise.FromResult(new NoOverloadFunctionSymbol(0) as FunctionSymbol),
                    ConstraintPromise.FromResult(new NoOverloadFunctionSymbol(0) as FunctionSymbol),
                    ConstraintPromise.FromResult(UndefinedMemberSymbol.Instance as Symbol));
            }

            // Look up the overload
            var getEnumeratorFunctions = GetFunctions(getEnumeratorResult);
            var getEnumeratorPromise = constraints.Overload(
                "GetEnumerator",
                getEnumeratorFunctions,
                ImmutableArray<object>.Empty,
                out var enumeratorType,
                syntax.Sequence);

            // Look up MoveNext
            var moveNextMethodsPromise = constraints.Member(enumeratorType, "MoveNext", out _, syntax.Sequence);

            var moveNextPromise = constraints.Await(moveNextMethodsPromise, () =>
            {
                var moveNextResult = moveNextMethodsPromise.Result;

                // Don't propagate errors
                if (moveNextResult.IsError)
                {
                    return ConstraintPromise.FromResult(new NoOverloadFunctionSymbol(0) as FunctionSymbol);
                }

                var moveNextFunctions = GetFunctions(moveNextResult);

                var moveNextPromise = constraints.Overload(
                    "MoveNext",
                    moveNextFunctions,
                    ImmutableArray<object>.Empty,
                    out var moveNextReturnType,
                    syntax.Sequence);

                var moveNextReturnsBoolPromise = constraints.SameType(
                    this.IntrinsicSymbols.Bool,
                    moveNextReturnType,
                    syntax.Sequence);

                return moveNextPromise;
            }).Unwrap();

            // Look up Current
            var currentPromise = constraints.Member(
                enumeratorType,
                "Current",
                out var currentType,
                syntax.Sequence);

            var elementAssignablePromise = constraints.Assignable(
                elementType,
                currentType,
                syntax.ElementType as SyntaxNode ?? syntax.Iterator);

            // Current needs to be a gettable property
            constraints.Await(currentPromise, () =>
            {
                var current = currentPromise.Result;

                // Don't propagate error
                if (current.IsError) return default(Unit);

                if (current is not PropertySymbol propSymbol || propSymbol.Getter is null)
                {
                    diagnostics.Add(Diagnostic.Create(
                        template: SymbolResolutionErrors.NotGettableProperty,
                        location: syntax.Sequence.Location,
                        formatArgs: current.Name));
                }

                return default;
            });

            return new BoundForExpression(
                syntax,
                iterator,
                sequence,
                then,
                continueLabel,
                breakLabel,
                getEnumeratorPromise,
                moveNextPromise,
                currentPromise);
        });
        return new BoundDelayedExpression(syntax, exprPromise, IntrinsicSymbols.Unit);
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindCallExpression(CallExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // TODO: We might just be able to inline the method below to here
        var method = this.BindExpression(syntax.Function, constraints, diagnostics);
        var args = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToList();

        return this.BindCallExpression(syntax, method, args, constraints, diagnostics);
    }

    private BindingTask<BoundExpression> BindCallExpression(
        CallExpressionSyntax syntax,
        BindingTask<BoundExpression> method,
        IEnumerable<BindingTask<BoundExpression>> args,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
#if false
        if (method is BoundDelayedExpression delayed)
        {
            // The binding is delayed, we have to delay this as well
            var promisedType = constraints.AllocateTypeVariable();
            var promise = constraints.Await(delayed.Promise, () =>
            {
                // Retry binding with the resolved variant
                var call = this.BindCallExpression(syntax, delayed.Promise.Result, args, constraints, diagnostics);
                ConstraintSolver.UnifyAsserted(promisedType, call.TypeRequired);
                return call;
            });
            return new BoundDelayedExpression(syntax, promise, promisedType);
        }
        else if (method is BoundFunctionGroupExpression group)
        {
            // Simple overload
            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                group.Functions[0].Name,
                group.Functions,
                args.Cast<object>().ToImmutableArray(),
                out var resultType,
                syntax.Function);

            return new BoundCallExpression(syntax, null, symbolPromise, args, resultType);
        }
        else if (method is BoundMemberExpression mem)
        {
            // We are in a bit of a pickle here, the member expression might not be resolved yet,
            // and based on it this can be a direct, or indirect call
            // If the resolved members are a statically bound function symbols, this becomes an overloaded call,
            // otherwise this becomes an indirect call

            var promisedType = constraints.AllocateTypeVariable();
            var promise = constraints.Await(mem.Member, BoundExpression () =>
            {
                var members = mem.Member.Result;
                if (members is FunctionSymbol or OverloadSymbol)
                {
                    // Overloaded member call
                    var functions = GetFunctions(members);
                    var symbolPromise = constraints.Overload(
                        members.Name,
                        functions,
                        args.Cast<object>().ToImmutableArray(),
                        out var resultType,
                        syntax.Function);

                    ConstraintSolver.UnifyAsserted(resultType, promisedType);
                    return new BoundCallExpression(syntax, mem.Accessed, symbolPromise, args, resultType);
                }
                else
                {
                    var callPromise = constraints.Call(
                        method.TypeRequired,
                        args.Cast<object>().ToImmutableArray(),
                        out var resultType,
                        syntax);

                    ConstraintSolver.UnifyAsserted(resultType, promisedType);
                    return new BoundIndirectCallExpression(syntax, mem, args, resultType);
                }
            });
            return new BoundDelayedExpression(syntax, promise, promisedType);
        }
        else
        {
            var callPromise = constraints.Call(
                method.TypeRequired,
                args.Cast<object>().ToImmutableArray(),
                out var resultType,
                syntax);
            return new BoundIndirectCallExpression(syntax, method, args, resultType);
        }
#else
        throw new NotImplementedException();
#endif
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
            ImmutableArray.Create<object>(operandTask),
            out var resultType,
            syntax.Operator);

        return new BoundUnaryExpression(syntax, await symbolPromise, await operandTask, resultType);
    }

    private async BindingTask<BoundExpression> BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var leftTask = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Right must be assignable to left
            _ = constraints.Assignable(
                leftTask.GetResultTypeRequired(constraints),
                rightTask.GetResultTypeRequired(constraints),
                syntax);

            return new BoundAssignmentExpression(syntax, null, await leftTask, await rightTask);
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var leftTask = this.BindExpression(syntax.Left, constraints, diagnostics);
            var rightTask = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Both left and right must be bool
            _ = constraints.SameType(
                this.IntrinsicSymbols.Bool,
                leftTask.GetResultTypeRequired(constraints),
                syntax.Left);
            _ = constraints.SameType(
                this.IntrinsicSymbols.Bool,
                rightTask.GetResultTypeRequired(constraints),
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
                ImmutableArray.Create<object>(leftTask, rightTask),
                out var resultType,
                syntax.Operator);
            // The result of the binary operator must be assignable to the left-hand side
            // For example, a + b in the form of a += b means that a + b has to result in a type
            // that is assignable to a, hence the extra constraint
            _ = constraints.Assignable(
                leftTask.GetResultTypeRequired(constraints),
                resultType,
                syntax);

            return new BoundAssignmentExpression(syntax, await symbolPromise, await leftTask, await rightTask);
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
                ImmutableArray.Create<object>(leftTask, rightTask),
                out var resultType,
                syntax.Operator);

            return new BoundBinaryExpression(syntax, await symbolPromise, await leftTask, await rightTask, resultType);
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
            ImmutableArray.Create<object>(left, right),
            out var resultType,
            syntax.Operator);
        // For safety, we assume it has to be bool
        _ = constraints.SameType(this.IntrinsicSymbols.Bool, resultType, syntax.Operator);

        return new BoundComparison(syntax, await symbolPromise, await right);
    }

    private BindingTask<BoundExpression> BindMemberExpression(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var left = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;
        if (left is BoundReferenceErrorExpression err)
        {
            // Error, don't cascade
            return new BoundReferenceErrorExpression(syntax, err.Symbol);
        }
        Symbol? container = left is BoundModuleExpression untypedModule
            ? untypedModule.Module
            : (left as BoundTypeExpression)?.Type;

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
            var promise = constraints.Member(left.TypeRequired, memberName, out var memberType, syntax);
            return new BoundMemberExpression(syntax, left, promise, memberType);
        }
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindIndexExpression(IndexExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var receiver = this.BindExpression(syntax.Indexed, constraints, diagnostics);
        if (receiver is BoundReferenceErrorExpression err)
        {
            return new BoundReferenceErrorExpression(syntax, err.Symbol);
        }
        var args = syntax.IndexList.Values.Select(x => this.BindExpression(x, constraints, diagnostics)).ToImmutableArray();
        var returnType = constraints.AllocateTypeVariable();
        var promise = constraints.Substituted(receiver.TypeRequired, () =>
        {
            var receiverType = receiver.TypeRequired.Substitution;

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
                ConstraintSolver.UnifyAsserted(returnType, IntrinsicSymbols.ErrorType);
                return ConstraintPromise.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(args.Length));
            }
            var overloaded = constraints.Overload(
                "operator[]",
                indexers,
                args.Cast<object>().ToImmutableArray(),
                out var gotReturnType,
                syntax);
            ConstraintSolver.UnifyAsserted(returnType, gotReturnType);
            return overloaded;
        }, syntax).Unwrap();

        return new BoundIndexGetExpression(syntax, receiver, promise, args, returnType);
#else
        throw new NotImplementedException();
#endif
    }

    private BindingTask<BoundExpression> BindGenericExpression(GenericExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
#if false
        var instantiated = this.BindExpression(syntax.Instantiated, constraints, diagnostics);
        var args = syntax.Arguments.Values
            .Select(arg => this.BindTypeToTypeSymbol(arg, diagnostics))
            .ToImmutableArray();
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
                return new BoundFunctionGroupExpression(syntax, instantiatedFuncs);
            }
        }
        else if (instantiated is BoundMemberExpression member)
        {
            // We are playing the same game as with call expression
            // A member access has to be delayed to get resolved

            var promise = constraints.Await(member.Member, BoundExpression () =>
            {
                var members = member.Member.Result;
                // Search for all function members with the same number of generic parameters
                var withSameNoParams = GetFunctions(members);
                if (withSameNoParams.Length == 0)
                {
                    // No generic functions with this number of parameters
                    diagnostics.Add(Diagnostic.Create(
                        template: TypeCheckingErrors.NoGenericFunctionWithParamCount,
                        location: syntax.Location,
                        formatArgs: new object[] { members.Name, args.Length }));

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
                    var overload = new OverloadSymbol(instantiatedFuncs);

                    // Wrap them back up in a member expression
                    return new BoundMemberExpression(
                        syntax,
                        member.Accessed,
                        ConstraintPromise.FromResult<Symbol>(overload),
                        member.Type);
                }
            });
            // NOTE: The generic function itself has no concrete type
            return new BoundDelayedExpression(syntax, promise, IntrinsicSymbols.ErrorType);
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
#else
        throw new NotImplementedException();
#endif
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
#if false
            return new BoundModuleExpression(syntax, module);
#else
            throw new NotImplementedException();
#endif
        case TypeSymbol type:
            // NOTE: Hack, see the note above this method definition
            this.BindTypeSyntaxToSymbol(syntax, type);
#if false
            return new BoundTypeExpression(syntax, type);
#else
            throw new NotImplementedException();
#endif
        case ParameterSymbol param:
            return new BoundParameterExpression(syntax, param);
        case UntypedLocalSymbol local:
#if false
            return new BoundLocalExpression(syntax, local, constraints.GetLocalType(local));
#else
            throw new NotImplementedException();
#endif
        case GlobalSymbol global:
            return new BoundGlobalExpression(syntax, global);
        case PropertySymbol prop:
            var getter = GetGetterSymbol(syntax, prop, diagnostics);
            return new BoundPropertyGetExpression(syntax, null, getter);
        case FunctionSymbol func:
            return new BoundFunctionGroupExpression(syntax, ImmutableArray.Create(func));
        case OverloadSymbol overload:
            return new BoundFunctionGroupExpression(syntax, overload.Functions);
        default:
            throw new InvalidOperationException();
        }
    }

    private static ImmutableArray<FunctionSymbol> GetFunctions(Symbol symbol) => symbol switch
    {
        FunctionSymbol f => ImmutableArray.Create(f),
        OverloadSymbol o => o.Functions,
        _ => ImmutableArray<FunctionSymbol>.Empty,
    };

    private static ExpressionSyntax ExtractValueSyntax(ExpressionSyntax syntax) => syntax switch
    {
        IfExpressionSyntax @if => ExtractValueSyntax(@if.Then),
        WhileExpressionSyntax @while => ExtractValueSyntax(@while.Then),
        ForExpressionSyntax @for => ExtractValueSyntax(@for.Then),
        BlockExpressionSyntax block => block.Value is null ? block : ExtractValueSyntax(block.Value),
        _ => syntax,
    };
}
