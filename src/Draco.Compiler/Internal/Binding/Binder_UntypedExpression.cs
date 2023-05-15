using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.UntypedTree;

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
        CallExpressionSyntax call => this.BindCallExpression(call, constraints, diagnostics),
        UnaryExpressionSyntax ury => this.BindUnaryExpression(ury, constraints, diagnostics),
        BinaryExpressionSyntax bin => this.BindBinaryExpression(bin, constraints, diagnostics),
        RelationalExpressionSyntax rel => this.BindRelationalExpression(rel, constraints, diagnostics),
        MemberExpressionSyntax maccess => this.BindMemberExpression(maccess, constraints, diagnostics),
        GenericExpressionSyntax gen => this.BindGenericExpression(gen, constraints, diagnostics),
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

    private UntypedExpression BindLiteralExpression(LiteralExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics) =>
        new UntypedLiteralExpression(syntax, syntax.Literal.Value);

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
                var text = content.Content.ValueText;
                if (text is null) throw new InvalidOperationException();
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
        return new UntypedStringExpression(syntax, parts.ToImmutable());
    }

    private UntypedExpression BindNameExpression(NameExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var symbol = this.LookupValueSymbol(syntax.Name.Text, syntax, diagnostics);
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
        constraints
            .SameType(IntrinsicSymbols.Bool, condition.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));

        var then = this.BindExpression(syntax.Then, constraints, diagnostics);
        var @else = syntax.Else is null
            ? UntypedUnitExpression.Default
            : this.BindExpression(syntax.Else.Expression, constraints, diagnostics);

        // Then and else must be compatible types
        var resultType = constraints.AllocateTypeVariable();
        constraints
            .CommonType(resultType, ImmutableArray.Create(then.TypeRequired, @else.TypeRequired))
            .ConfigureDiagnostic(diag =>
            {
                // The location will point at the else value, assuming that the latter expression is
                // the offending one
                // If there is no else clause, we just point at the then clause
                diag.WithLocation(syntax.Else is null
                    ? ExtractValueSyntax(syntax.Then).Location
                    : ExtractValueSyntax(syntax.Else.Expression).Location);
                // If there is an else clause, we annotate the then clause as related info
                diag.WithRelatedInformation(
                    format: "the other branch is inferred to be {0}",
                    formatArgs: then.TypeRequired,
                    location: ExtractValueSyntax(syntax.Then).Location);
            });

        return new UntypedIfExpression(syntax, condition, then, @else, resultType);
    }

    private UntypedExpression BindWhileExpression(WhileExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var binder = this.GetBinder(syntax);

        var condition = binder.BindExpression(syntax.Condition, constraints, diagnostics);
        // Condition must be bool
        constraints
            .SameType(IntrinsicSymbols.Bool, condition.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));

        var then = binder.BindExpression(syntax.Then, constraints, diagnostics);
        // Body must be unit
        constraints
            .SameType(IntrinsicSymbols.Unit, then.TypeRequired)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(ExtractValueSyntax(syntax.Then).Location));

        // Resolve labels
        var continueLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "continue");
        var breakLabel = binder.DeclaredSymbols
            .OfType<LabelSymbol>()
            .First(sym => sym.Name == "break");

        return new UntypedWhileExpression(syntax, condition, then, continueLabel, breakLabel);
    }

    private UntypedExpression BindCallExpression(CallExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        var method = this.BindExpression(syntax.Function, constraints, diagnostics);
        var args = syntax.ArgumentList.Values
            .Select(arg => this.BindExpression(arg, constraints, diagnostics))
            .ToImmutableArray();

        if (method is UntypedFunctionGroupExpression group)
        {
            // Simple overload
            // Resolve symbol overload
            var symbolPromise = constraints.Overload(
                group.Functions,
                args.Select(arg => arg.TypeRequired).ToImmutableArray(),
                out var resultType);
            symbolPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Function.Location));

            return new UntypedCallExpression(syntax, null, symbolPromise, args, resultType);
        }
        else if (method is UntypedMemberExpression mem)
        {
            // We are in a bit of a pickle here, the member expression might not be resolved yet,
            // and based on it this can be a direct, or indirect call
            // If the resolved members are a statically bound function symbols, this becomes an overloaded call,
            // otherwise this becomes an indirect call

            var promisedType = constraints.AllocateTypeVariable();
            var promise = constraints.Await(mem.Member, UntypedExpression (members) =>
            {
                if (members.All(m => m is FunctionSymbol))
                {
                    // Overloaded member call
                    var funcMembers = members
                        .Cast<FunctionSymbol>()
                        .ToImmutableArray();

                    var symbolPromise = constraints.Overload(
                        funcMembers,
                        args.Select(arg => arg.TypeRequired).ToImmutableArray(),
                        out var resultType);
                    symbolPromise.ConfigureDiagnostic(diag => diag
                        .WithLocation(syntax.Function.Location));

                    constraints.Unify(resultType, promisedType);
                    return new UntypedCallExpression(syntax, mem.Accessed, symbolPromise, args, resultType);
                }
                else if (members.Length == 1)
                {
                    var callPromise = constraints.Call(
                        method.TypeRequired,
                        args.Select(arg => arg.TypeRequired).ToImmutableArray(),
                        out var resultType);
                    callPromise.ConfigureDiagnostic(diag => diag
                        .WithLocation(syntax.Location));
                    return new UntypedIndirectCallExpression(syntax, mem, args, resultType);
                }
                else
                {
                    // NOTE: Can this happen?
                    // Maybe it can on cascaded errors, like duplicate member definitions
                    // TODO: Verify
                    throw new NotImplementedException();
                }
            });
            return new UntypedDelayedExpression(syntax, promise, promisedType);
        }
        else
        {
            var callPromise = constraints.Call(
                method.TypeRequired,
                args.Select(arg => arg.TypeRequired).ToImmutableArray(),
                out var resultType);
            callPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));
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
            GetFunctions(operatorSymbol),
            ImmutableArray.Create(operand.TypeRequired),
            out var resultType);
        symbolPromise.ConfigureDiagnostic(diag => diag
            .WithLocation(syntax.Operator.Location));

        return new UntypedUnaryExpression(syntax, symbolPromise, operand, resultType);
    }

    private UntypedExpression BindBinaryExpression(BinaryExpressionSyntax syntax, ConstraintSolver constraints, DiagnosticBag diagnostics)
    {
        if (syntax.Operator.Kind == TokenKind.Assign)
        {
            var left = this.BindLvalue(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Right must be assignable to left
            constraints
                .Assignable(left.Type, right.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location));

            return new UntypedAssignmentExpression(syntax, null, left, right);
        }
        else if (syntax.Operator.Kind is TokenKind.KeywordAnd or TokenKind.KeywordOr)
        {
            var left = this.BindExpression(syntax.Left, constraints, diagnostics);
            var right = this.BindExpression(syntax.Right, constraints, diagnostics);

            // Both left and right must be bool
            constraints
                .SameType(IntrinsicSymbols.Bool, left.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Left.Location));
            constraints
                .SameType(IntrinsicSymbols.Bool, right.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Right.Location));

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
                GetFunctions(operatorSymbol),
                ImmutableArray.Create(left.Type, right.TypeRequired),
                out var resultType);
            symbolPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));
            // The result of the binary operator must be assignable to the left-hand side
            // For example, a + b in the form of a += b means that a + b has to result in a type
            // that is assignable to a, hence the extra constraint
            constraints
                .Assignable(left.Type, resultType)
                .ConfigureDiagnostic(diag => diag
                    .WithLocation(syntax.Location));

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
                GetFunctions(operatorSymbol),
                ImmutableArray.Create(left.TypeRequired, right.TypeRequired),
                out var resultType);
            symbolPromise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));

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
        return new UntypedRelationalExpression(syntax, first, comparisons.ToImmutable());
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
            GetFunctions(operatorSymbol),
            ImmutableArray.Create(prev.TypeRequired, right.TypeRequired),
            out var resultType);
        symbolPromise.ConfigureDiagnostic(diag => diag
            .WithLocation(syntax.Operator.Location));
        // For safety, we assume it has to be bool
        constraints
            .SameType(IntrinsicSymbols.Bool, resultType)
            .ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Operator.Location));

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
        else if (left is UntypedModuleExpression moduleExpr)
        {
            // Module member access
            var module = moduleExpr.Module;
            var members = module.Members
                .Where(m => m.Name == memberName)
                .Where(BinderFacts.IsValueSymbol)
                .ToImmutableArray();
            // Reuse logic from LookupResult
            var result = LookupResult.FromResultSet(members);
            var symbol = result.GetValue(memberName, syntax, diagnostics);
            return this.SymbolToExpression(syntax, symbol, constraints, diagnostics);
        }
        else
        {
            // Value, add constraint
            var promise = constraints.Member(left.TypeRequired, memberName);
            promise.ConfigureDiagnostic(diag => diag
                .WithLocation(syntax.Location));
            return new UntypedMemberExpression(syntax, left, promise);
        }
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
                // TODO
                throw new NotImplementedException();
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
        else
        {
            // TODO
            throw new NotImplementedException();
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
            // NOTE: Hack, see the node above this method definition
            this.BindModuleSyntaxToSymbol(syntax, module);
            return new UntypedModuleExpression(syntax, module);
        case ParameterSymbol param:
            return new UntypedParameterExpression(syntax, param);
        case UntypedLocalSymbol local:
            return new UntypedLocalExpression(syntax, local, constraints.GetLocalType(local));
        case GlobalSymbol global:
            return new UntypedGlobalExpression(syntax, global);
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
        _ => throw new ArgumentOutOfRangeException(nameof(symbol)),
    };

    private static ExpressionSyntax ExtractValueSyntax(ExpressionSyntax syntax) => syntax switch
    {
        IfExpressionSyntax @if => ExtractValueSyntax(@if.Then),
        WhileExpressionSyntax @while => ExtractValueSyntax(@while.Then),
        BlockExpressionSyntax block => block.Value is null ? block : ExtractValueSyntax(block.Value),
        _ => syntax,
    };
}
