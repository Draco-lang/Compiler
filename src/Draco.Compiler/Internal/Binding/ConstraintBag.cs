using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.UntypedTree;
using Diagnostic = Draco.Compiler.Internal.Diagnostics.Diagnostic;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a set of constraints from the typesystem.
/// </summary>
internal sealed class ConstraintBag
{
    /// <summary>
    /// The solved behind the constraint bag.
    /// </summary>
    public ConstraintSolver Solver { get; } = new();

    private readonly Dictionary<UntypedLocalSymbol, Type> localTypes = new();
    private readonly Dictionary<UntypedLocalSymbol, SourceLocalSymbol> mappedLocals = new();

    /// <summary>
    /// Maps the untyped local to a well-typed local symbol.
    /// </summary>
    /// <param name="diagnostics">The diagnostics during inference.</param>
    /// <param name="symbol">The untyped local to map.</param>
    /// <returns>The mapped, well-typed local symbol.</returns>
    public LocalSymbol GetTypedLocal(DiagnosticBag diagnostics, UntypedLocalSymbol symbol)
    {
        if (!this.mappedLocals.TryGetValue(symbol, out var typedSymbol))
        {
            var localType = this.Solver.Unwrap(this.localTypes[symbol]);
            if (localType.IsTypeVariable)
            {
                // We could not infer the type
                diagnostics.Add(Diagnostic.Create(
                    template: TypeCheckingErrors.CouldNotInferType,
                    // TODO: Ugly location API
                    location: new Internal.Diagnostics.Location.TreeReference(symbol.DeclarationSyntax),
                    formatArgs: symbol.Name));
                // We use an error type
                localType = Types.Intrinsics.Error;
            }
            typedSymbol = new(symbol, localType);
            this.mappedLocals.Add(symbol, typedSymbol);
        }
        return typedSymbol;
    }

    /// <summary>
    /// Adds a constraint that declared the local with the given type and given initial value.
    /// </summary>
    /// <param name="symbol">The symbol for the local.</param>
    /// <param name="type">The declared type of the local.</param>
    /// <param name="value">The declared initial value of the local.</param>
    /// <param name="syntax">The syntax declaring the local.</param>
    public void LocalDeclaration(UntypedLocalSymbol symbol, Symbol? type, UntypedExpression? value, VariableDeclarationSyntax syntax)
    {
        // If the type is not specified, we assume it still can be anything
        var inferredType = type is null
            ? this.Solver.NextTypeVariable
            : ((TypeSymbol)type).Type;
        this.localTypes.Add(symbol, inferredType);

        // If there's a value, it has to be assignable
        if (value is not null)
        {
            this.Solver
                .Assignable(inferredType, value.TypeRequired)
                .ConfigureDiagnostic(diag => diag
                    // TODO: This is a horrible way to set the reference...
                    // We should definitely rework the location API...
                    .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)));
        }
    }

    /// <summary>
    /// References a local.
    /// </summary>
    /// <param name="local">The local symbol being referenced.</param>
    /// <param name="syntax">The referencing syntax.</param>
    /// <returns>The type of the local that can be used for further typing rules.</returns>
    public Type LocalReference(UntypedLocalSymbol local, NameExpressionSyntax syntax) => this.localTypes[local];

    /// <summary>
    /// Enforces a type to be a boolean for a condition.
    /// </summary>
    /// <param name="expr">The expression that has to be enforced.</param>
    public void IsCondition(UntypedExpression expr)
    {
        Debug.Assert(expr.Syntax is not null);
        this.Solver
            .SameType(expr.TypeRequired, Types.Intrinsics.Bool)
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(expr.Syntax)));
    }

    /// <summary>
    /// Enforces a type to be unit.
    /// </summary>
    /// <param name="expr">The expression that has to be enforced.</param>
    public void IsUnit(UntypedExpression expr)
    {
        Debug.Assert(expr.Syntax is not null);
        this.Solver
            .SameType(expr.TypeRequired, Types.Intrinsics.Unit)
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(expr.Syntax)));
    }

    /// <summary>
    /// Enforces two expressions to have compatible types (for an if-expression for example).
    /// </summary>
    /// <param name="first">The first expression.</param>
    /// <param name="second">The second expression.</param>
    /// <returns>A type that can be used to reference the common type of <paramref name="first"/>
    /// and <paramref name="second"/>.</returns>
    public Type CommonType(UntypedExpression first, UntypedExpression second)
    {
        var firstType = first.TypeRequired;
        var secondType = second.TypeRequired;
        // Optimization: if the left and right reference the same type, we know they are the common type
        if (ReferenceEquals(firstType, secondType)) return firstType;
        // Add constraint
        return this.Solver
            .CommonType(firstType, secondType)
            // TODO: We should extract syntax here to point to inferred common types
            // Like in if-else branches
            .ConfigureDiagnostic(diag => { })
            .Result;
    }

    /// <summary>
    /// Constraints that an expression is assignable to an lvalue.
    /// </summary>
    /// <param name="left">The lvalue to assign to.</param>
    /// <param name="right">The expression to assign.</param>
    /// <param name="syntax">The assignment syntax.</param>
    public void IsAssignable(UntypedLvalue left, UntypedExpression right, BinaryExpressionSyntax syntax)
    {
        var leftType = left.Type;
        var rightType = right.TypeRequired;
        // Optimization: if the left and right reference the same type, we know it's assignable
        if (ReferenceEquals(leftType, rightType)) return;
        // Add constraint
        this.Solver
            .Assignable(leftType, rightType)
            // TODO: Ugly location API
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)));
    }

    /// <summary>
    /// Constraints that an expression is returnable from a function.
    /// </summary>
    /// <param name="value">The returned value.</param>
    /// <param name="function">The function being returned from.</param>
    /// <param name="syntax">The syntax returning.</param>
    public void Return(UntypedExpression value, FunctionSymbol function, SyntaxNode syntax)
    {
        var returnType = function.ReturnType;
        var valueType = value.TypeRequired;
        // Optimization: if the left and right reference the same type, we know it's assignable
        if (ReferenceEquals(returnType, valueType)) return;
        // TODO: Maybe not the correct constraint
        this.Solver
            .Assignable(returnType, valueType)
            // TODO: Ugly location API
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)));
    }

    /// <summary>
    /// Constraints that a function is being invoked.
    /// </summary>
    /// <param name="method">The called method expression.</param>
    /// <param name="args">The arguments the method is called with.</param>
    /// <param name="syntax">The syntax invoking the function.</param>
    /// <returns>A type that can be used to reference the result of the function invocation.</returns>
    internal Type CallFunction(UntypedExpression method, ImmutableArray<UntypedExpression> args, CallExpressionSyntax syntax)
    {
        var methodType = method.TypeRequired;
        var argumentTypes = args.Select(arg => arg.TypeRequired);
        return this.Solver
            .Call(methodType, argumentTypes)
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)))
            .Result;
    }

    /// <summary>
    /// Constraints that an unary operator is being invoked.
    /// </summary>
    /// <param name="operator">The operator symbol.</param>
    /// <param name="operand">The operand.</param>
    /// <param name="syntax">The syntax invoking the operator.</param>
    /// <returns>A pair of the operator symbol promise and the return-type.</returns>
    public (ConstraintPromise<FunctionSymbol> Symbol, Type ReturnType) CallUnaryOperator(
        Symbol @operator,
        UntypedExpression operand,
        UnaryExpressionSyntax syntax) =>
        throw new System.NotImplementedException();

    /// <summary>
    /// Constraints that a binary operator is being invoked.
    /// </summary>
    /// <param name="operator">The operator symbol.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="syntax">The syntax invoking the operator.</param>
    /// <returns>A pair of the operator symbol promise and the return-type.</returns>
    public (ConstraintPromise<FunctionSymbol> Symbol, Type ReturnType) CallBinaryOperator(
        Symbol @operator,
        UntypedExpression left,
        UntypedExpression right,
        BinaryExpressionSyntax syntax)
    {
        // TODO: This promise isn't configured with diagnostics
        var (promise, callSite) = this.Overload(@operator);
        var returnType = this.Solver
            .Call(callSite, new[] { left.TypeRequired, right.TypeRequired })
            .ConfigureDiagnostic(diag => diag
            // TODO: This is a horrible way to set the reference...
            // We should definitely rework the location API...
            .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)))
            .Result;
        return (promise, returnType);
    }

    /// <summary>
    /// Constraints that a comparison operator is being invoked.
    /// </summary>
    /// <param name="operator">The operator symbol.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="syntax">The syntax invoking the operator.</param>
    /// <returns>The symbol promise.</returns>
    public ConstraintPromise<FunctionSymbol> CallComparisonOperator(
        Symbol @operator,
        UntypedExpression left,
        UntypedExpression right,
        ComparisonElementSyntax syntax) =>
        throw new System.NotImplementedException();

    // Utility to retrieve the promise and function type of a potential overload
    private (ConstraintPromise<FunctionSymbol> Symbol, Type CallSite) Overload(Symbol symbol) => symbol switch
    {
        FunctionSymbol function => (ConstraintPromise.FromResult(function), function.Type),
        OverloadSymbol overload => this.Solver.Overload(overload.Functions),
        _ => throw new System.InvalidOperationException(),
    };
}
