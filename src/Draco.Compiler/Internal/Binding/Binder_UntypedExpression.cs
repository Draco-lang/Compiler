using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;
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
    protected virtual UntypedExpression BindExpression(SyntaxNode syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) => syntax switch
    {
        // NOTE: The syntax error is already reported
        UnexpectedExpressionSyntax => new UntypedUnexpectedExpression(syntax),
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

    private UntypedExpression BindStatementExpression(StatementExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // We just desugar stmt; into { stmt; }
        var stmt = this.BindStatement(syntax.Statement, constraints, diagnostics);
        return new UntypedBlockExpression(
            syntax: syntax,
            locals: ImmutableArray<UntypedLocalSymbol>.Empty,
            statements: ImmutableArray.Create(stmt),
            value: UntypedUnitExpression.Default);
    }

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (!BinderFacts.TryGetLiteralType(syntax.Literal.Value, this.IntrinsicSymbols, out var literalType))
        {
            throw new InvalidOperationException("can not determine literal type");
        }
        return new UntypedLiteralExpression(syntax, syntax.Literal.Value, literalType);
    }

    private UntypedExpression BindStringExpression(StringExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        static string ComputeCutoff(StringExpressionSyntax str)
        {
            // Line strings have no cutoff
            if (str.OpenQuotes.Kind == TokenKind.LineStringStart) return string.Empty;
            // Multiline strings
            Debug.Assert(str.CloseQuotes.LeadingTrivia.Count <= 2);
            // If this is true, we have malformed input
            if (str.CloseQuotes.LeadingTrivia.Count == 0) return string.Empty;
            // If this is true, there's only newline, no spaces before
            if (str.CloseQuotes.LeadingTrivia.Count == 1) return string.Empty;
            // The first trivia was newline, the second must be spaces
            Debug.Assert(str.CloseQuotes.LeadingTrivia[1].Kind == TriviaKind.Whitespace);
            return str.CloseQuotes.LeadingTrivia[1].Text;
        }

        var lastNewline = true;
        var cutoff = ComputeCutoff(syntax);
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
        return new UntypedStringExpression(syntax, parts.ToImmutable(), this.IntrinsicSymbols.String);
    }

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = BinderFacts.SyntaxMustNotReferenceTypes(syntax)
            ? this.LookupNonTypeValueSymbol(syntax.Name.Text, syntax, diagnostics)
            : this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
        return this.SymbolToExpression(syntax, symbol, constraints, diagnostics);
    }

    private UntypedExpression BindBlockExpression(BlockExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);
        var locals = binder.DeclaredSymbols
            .OfType<UntypedLocalSymbol>()
            .ToImmutableArray();
        var statements = syntax.Statements
            .Select(s => binder.BindStatement(s, constraints, diagnostics))
            .ToImmutableArray();
        var value = syntax.Value is null
            ? UntypedUnitExpression.Default
            : binder.BindExpression(syntax.Value, constraints, diagnostics);
        return new UntypedBlockExpression(syntax, locals, statements, value);
    }

    private UntypedExpression BindGotoExpression(GotoExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var target = (LabelSymbol)this.BindLabel(syntax.Target, constraints, diagnostics);
        return new UntypedGotoExpression(syntax, target);
    }

    private UntypedExpression BindReturnExpression(ReturnExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var value = syntax.Value is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Value, constraints, diagnostics);

        this.ConstraintReturnType(syntax, value, constraints);

        return new UntypedReturnExpression(syntax, value);
    }

    private UntypedExpression BindIfExpression(IfExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var condition = this.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        constraints.SameType(this.IntrinsicSymbols.Bool, condition.TypeRequired, syntax);

        var then = this.BindExpression(syntax.Then, constraints, diagnostics);
        var @else = syntax.Else is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        // Then and else must be compatible types
        var resultType = constraints.AllocateTypeVariable();
        constraints.CommonType(
            resultType,
            ImmutableArray.Create(then.TypeRequired, @else.TypeRequired),
            // The location will point at the else value, assuming that the latter expression is
            // the offending one
            // If there is no else clause, we just point at the then clause
            ConstraintLocator.Syntax(syntax.Else is null
                ? ExtractValueSyntax(syntax.Then)
                : ExtractValueSyntax(syntax.Else.Expression))
                .WithRelatedInformation(
                    format: "the other branch is inferred to be {0}",
                    formatArgs: then.TypeRequired,
                    // If there is an else clause, we annotate the then clause as related info
                    location: ExtractValueSyntax(syntax.Then).Location));

        return new UntypedIfExpression(syntax, condition, then, @else, resultType);
    }

    private UntypedExpression BindWhileExpression(WhileExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
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

        return new UntypedWhileExpression(syntax, condition, then, continueLabel, breakLabel);
    }

    private UntypedExpression BindForExpression(ForExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
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

        var exprPromise = constraints.Await(getEnumeratorMethodsPromise, UntypedExpression () =>
        {
            var getEnumeratorResult = getEnumeratorMethodsPromise.Result;
            if (getEnumeratorResult.IsError)
            {
                constraints.UnifyAsserted(elementType, IntrinsicSymbols.ErrorType);
                return new UntypedForExpression(
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

            return new UntypedForExpression(
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
        return new UntypedDelayedExpression(syntax, exprPromise, IntrinsicSymbols.Unit);
    }

    private UntypedExpression BindCallExpression(CallExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var method = this.BindExpression(syntax.Function, constraints, diagnostics);
        var args = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        return this.BindCallExpression(syntax, method, args, constraints, diagnostics);
    }

    private UntypedExpression BindCallExpression(
        CallExpressionSyntax syntax,
        UntypedExpression method,
        ImmutableArray<UntypedExpression> args,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        if (method is UntypedDelayedExpression delayed)
        {
            // The binding is delayed, we have to delay this as well
            var promisedType = constraints.AllocateTypeVariable();
            var promise = constraints.Await(delayed.Promise, () =>
            {
                // Retry binding with the resolved variant
                var call = this.BindCallExpression(syntax, delayed.Promise.Result, args, constraints, diagnostics);
                constraints.UnifyAsserted(promisedType, call.TypeRequired);
                return call;
            });
            return new UntypedDelayedExpression(syntax, promise, promisedType);
        }
        else if (method is UntypedFunctionGroupExpression group)
        {
            // Simple overload
            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                group.Functions[0].Name,
                group.Functions,
                args.Cast<object>().ToImmutableArray(),
                out var resultType,
                syntax.Function);

            return new UntypedCallExpression(syntax, null, symbolPromise, args, resultType);
        }
        else if (method is UntypedMemberExpression mem)
        {
            // We are in a bit of a pickle here, the member expression might not be resolved yet,
            // and based on it this can be a direct, or indirect call
            // If the resolved members are a statically bound function symbols, this becomes an overloaded call,
            // otherwise this becomes an indirect call

            var promisedType = constraints.AllocateTypeVariable();
            var promise = constraints.Await(mem.Member, UntypedExpression () =>
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

                    constraints.UnifyAsserted(resultType, promisedType);
                    return new UntypedCallExpression(syntax, mem.Accessed, symbolPromise, args, resultType);
                }
                else
                {
                    var callPromise = constraints.Call(
                        method.TypeRequired,
                        args.Cast<object>().ToImmutableArray(),
                        out var resultType,
                        syntax);

                    constraints.UnifyAsserted(resultType, promisedType);
                    return new UntypedIndirectCallExpression(syntax, mem, args, resultType);
                }
            });
            return new UntypedDelayedExpression(syntax, promise, promisedType);
        }
        else
        {
            var callPromise = constraints.Call(
                method.TypeRequired,
                args.Cast<object>().ToImmutableArray(),
                out var resultType,
                syntax);
            return new UntypedIndirectCallExpression(syntax, method, args, resultType);
        }
    }

    private UntypedExpression BindUnaryExpression(UnaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        // Get the unary operator symbol
        var operatorName = FunctionSymbol.GetUnaryOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var operand = this.BindExpression(syntax.Operand, constraints, diagnostics);

        // Resolve symbol overload
        var symbolPromise = constraints.Overload(
            operatorName,
            GetFunctions(operatorSymbol),
            ImmutableArray.Create<object>(operand),
            out var resultType,
            syntax.Operator);

        return new UntypedUnaryExpression(syntax, symbolPromise, operand, resultType);
    }

    private UntypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Right must be assignable to left
            constraints.Assignable(left.Type, right.TypeRequired, syntax);

            return new UntypedAssignmentExpression(syntax, null, left, right);
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Both left and right must be bool
            constraints.SameType(this.IntrinsicSymbols.Bool, left.TypeRequired, syntax.Left);
            constraints.SameType(this.IntrinsicSymbols.Bool, right.TypeRequired, syntax.Right);

            return syntax.Operator.Kind == TokenKind.KeywordAnd
                ? new UntypedAndExpression(syntax, left, right)
                : new UntypedOrExpression(syntax, left, right);
        }
        else if (SyntaxFacts.TryGetOperatorOfCompoundAssignment(syntax.Operator.Kind, out var nonCompound))
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(nonCompound);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);

            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                operatorName,
                GetFunctions(operatorSymbol),
                ImmutableArray.Create<object>(left, right),
                out var resultType,
                syntax.Operator);
            // The result of the binary operator must be assignable to the left-hand side
            // For example, a + b in the form of a += b means that a + b has to result in a type
            // that is assignable to a, hence the extra constraint
            constraints.Assignable(left.Type, resultType, syntax);

            return new UntypedAssignmentExpression(syntax, symbolPromise, left, right);
        }
        else
        {
            // Get the binary operator symbol
            var operatorName = FunctionSymbol.GetBinaryOperatorName(syntax.Operator.Kind);
            var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                operatorName,
                GetFunctions(operatorSymbol),
                ImmutableArray.Create<object>(left, right),
                out var resultType,
                syntax.Operator);

            return new UntypedBinaryExpression(syntax, symbolPromise, left, right, resultType);
        }
    }

    private UntypedExpression BindRelationalExpression(RelationalExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var first = this.BindExpression(syntax.Left, constraints, diagnostics);
        var comparisons = ImmutableArray.CreateBuilder<UntypedComparison>();
        var prev = first;
        foreach (var comparisonSyntax in syntax.Comparisons)
        {
            var comparison = this.BindComparison(prev, comparisonSyntax, constraints, diagnostics);
            prev = comparison.Next;
            comparisons.Add(comparison);
        }
        return new UntypedRelationalExpression(syntax, first, comparisons.ToImmutable(), this.IntrinsicSymbols.Bool);
    }

    private UntypedComparison BindComparison(
        UntypedExpression prev,
        ComparisonElementSyntax syntax,
        ConstraintSolver constraints,
        DiagnosticBag diagnostics)
    {
        // Get the comparison operator symbol
        var operatorName = FunctionSymbol.GetComparisonOperatorName(syntax.Operator.Kind);
        var operatorSymbol = this.LookupValueSymbol(operatorName, syntax, diagnostics);
        var right = this.BindExpression(syntax.Right, constraints, diagnostics);

        // NOTE: We know it must be bool, no need to pass it on to comparison
        // Resolve symbol overload
        var symbolPromise = constraints.Overload(
            operatorName,
            GetFunctions(operatorSymbol),
            ImmutableArray.Create<object>(prev, right),
            out var resultType,
            syntax.Operator);
        // For safety, we assume it has to be bool
        constraints.SameType(this.IntrinsicSymbols.Bool, resultType, syntax.Operator);

        return new UntypedComparison(syntax, symbolPromise, right);
    }

    private UntypedExpression BindMemberExpression(MemberExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var left = this.BindExpression(syntax.Accessed, constraints, diagnostics);
        var memberName = syntax.Member.Text;
        if (left is UntypedReferenceErrorExpression err)
        {
            // Error, don't cascade
            return new UntypedReferenceErrorExpression(syntax, err.Symbol);
        }
        Symbol? container = left is UntypedModuleExpression untypedModule
            ? untypedModule.Module
            : (left as UntypedTypeExpression)?.Type;

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
            return new UntypedMemberExpression(syntax, left, promise, memberType);
        }
    }

    private UntypedExpression BindIndexExpression(IndexExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var receiver = this.BindExpression(syntax.Indexed, constraints, diagnostics);
        if (receiver is UntypedReferenceErrorExpression err)
        {
            return new UntypedReferenceErrorExpression(syntax, err.Symbol);
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
                constraints.UnifyAsserted(returnType, IntrinsicSymbols.ErrorType);
                return ConstraintPromise.FromResult<FunctionSymbol>(new NoOverloadFunctionSymbol(args.Length));
            }
            var overloaded = constraints.Overload(
                "operator[]",
                indexers,
                args.Cast<object>().ToImmutableArray(),
                out var gotReturnType,
                syntax);
            constraints.UnifyAsserted(returnType, gotReturnType);
            return overloaded;
        }, syntax).Unwrap();

        return new UntypedIndexGetExpression(syntax, receiver, promise, args, returnType);
    }

    private UntypedExpression BindGenericExpression(GenericExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var instantiated = this.BindExpression(syntax.Instantiated, constraints, diagnostics);
        var args = syntax.Arguments.Values
            .Select(arg => this.BindTypeToTypeSymbol(arg, diagnostics))
            .ToImmutableArray();
        if (instantiated is UntypedFunctionGroupExpression group)
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
                return new UntypedReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
            }
            else
            {
                // There are functions with this same number of parameters
                // Instantiate each possibility
                var instantiatedFuncs = withSameNoParams
                    .Select(f => f.GenericInstantiate(f.ContainingSymbol, args))
                    .ToImmutableArray();

                // Wrap them back up in a function group
                return new UntypedFunctionGroupExpression(syntax, instantiatedFuncs);
            }
        }
        else if (instantiated is UntypedMemberExpression member)
        {
            // We are playing the same game as with call expression
            // A member access has to be delayed to get resolved

            var promise = constraints.Await(member.Member, UntypedExpression () =>
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
                    return new UntypedReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
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
                    return new UntypedMemberExpression(
                        syntax,
                        member.Accessed,
                        ConstraintPromise.FromResult<Symbol>(overload),
                        member.Type);
                }
            });
            // NOTE: The generic function itself has no concrete type
            return new UntypedDelayedExpression(syntax, promise, IntrinsicSymbols.ErrorType);
        }
        else
        {
            // Tried to instantiate something that can not be instantiated
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.NotGenericConstruct,
                location: syntax.Location));

            // Return a sentinel
            // NOTE: Is this the right one to return?
            return new UntypedReferenceErrorExpression(syntax, IntrinsicSymbols.ErrorType);
        }
    }

    private UntypedExpression SymbolToExpression(SyntaxNode syntax, Symbol symbol, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (symbol.IsError) return new UntypedReferenceErrorExpression(syntax, symbol);
        switch (symbol)
        {
        case Symbol when symbol.IsError:
            return new UntypedReferenceErrorExpression(syntax, symbol);
        case ModuleSymbol module:
            // NOTE: Hack, see the note above this method definition
            this.BindSyntaxToSymbol(syntax, module);
            return new UntypedModuleExpression(syntax, module);
        case TypeSymbol type:
            // NOTE: Hack, see the note above this method definition
            this.BindTypeSyntaxToSymbol(syntax, type);
            return new UntypedTypeExpression(syntax, type);
        case ParameterSymbol param:
            return new UntypedParameterExpression(syntax, param);
        case UntypedLocalSymbol local:
            return new UntypedLocalExpression(syntax, local, constraints.GetLocalType(local));
        case GlobalSymbol global:
            return new UntypedGlobalExpression(syntax, global);
        case PropertySymbol prop:
            var getter = GetGetterSymbol(syntax, prop, diagnostics);
            return new UntypedPropertyGetExpression(syntax, null, getter);
        case FunctionSymbol func:
            return new UntypedFunctionGroupExpression(syntax, ImmutableArray.Create(func));
        case OverloadSymbol overload:
            return new UntypedFunctionGroupExpression(syntax, overload.Functions);
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
