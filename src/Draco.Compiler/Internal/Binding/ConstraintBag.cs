using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
using Draco.Compiler.Internal.UntypedTree;

namespace Draco.Compiler.Internal.Binding;

/// <summary>
/// Represents a set of constraints from the typesystem.
/// </summary>
internal sealed class ConstraintBag
{
    private readonly ConstraintSolver solver = new();
    private readonly Dictionary<LocalSymbol, Type> localTypes = new();

    /// <summary>
    /// Adds a constraint that declared the local with the given type and given initial value.
    /// </summary>
    /// <param name="symbol">The symbol for the local.</param>
    /// <param name="type">The declared type of the local.</param>
    /// <param name="value">The declared initial value of the local.</param>
    /// <param name="syntax">The syntax declaring the local.</param>
    public void LocalDeclaration(LocalSymbol symbol, Symbol? type, UntypedExpression? value, VariableDeclarationSyntax syntax)
    {
        // If the type is not specified, we assume it still can be anything
        var inferredType = type is null
            ? this.solver.NextTypeVariable
            : ((TypeSymbol)type).Type;
        this.localTypes.Add(symbol, inferredType);

        // If there's a value, it has to be assignable
        if (value is not null)
        {
            this.solver
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
    public Type LocalReference(LocalSymbol local, NameExpressionSyntax syntax) => this.localTypes[local];

    /// <summary>
    /// Enforces a type to be a boolean for a condition.
    /// </summary>
    /// <param name="expr">The expression that has to be enforced.</param>
    public void IsCondition(UntypedExpression expr)
    {
        Debug.Assert(expr.Syntax is not null);
        this.solver
            .SameType(expr.TypeRequired, Intrinsics.Bool)
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
        this.solver
            .SameType(expr.TypeRequired, Intrinsics.Unit)
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
    public Type CommonType(UntypedExpression first, UntypedExpression second) =>
        throw new System.NotImplementedException();

    /// <summary>
    /// Constraints that an expression is assignable to an lvalue.
    /// </summary>
    /// <param name="left">The lvalue to assign to.</param>
    /// <param name="right">The expression to assign.</param>
    /// <param name="syntax">The assignment syntax.</param>
    public void IsAssignable(UntypedLvalue left, UntypedExpression right, BinaryExpressionSyntax syntax)
    {
        var leftType = left.Type;
        var rightType = right.Type;
        // Optimization: if the left and right reference the same type, we know it's assignable
        if (ReferenceEquals(leftType, rightType)) return;
        // TODO
        throw new System.NotImplementedException();
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
        return this.solver
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
    /// <returns>A type that can be used to reference the result of the operator invocation.</returns>
    public Type CallUnaryOperator(Symbol @operator, UntypedExpression operand, UnaryExpressionSyntax syntax) =>
        throw new System.NotImplementedException();

    /// <summary>
    /// Constraints that a binary operator is being invoked.
    /// </summary>
    /// <param name="operator">The operator symbol.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="syntax">The syntax invoking the operator.</param>
    /// <returns>A type that can be used to reference the result of the operator invocation.</returns>
    public Type CallBinaryOperator(Symbol @operator, UntypedExpression left, UntypedExpression right, BinaryExpressionSyntax syntax)
    {
        var functionSymbol = (FunctionSymbol)@operator;
        var methodType = new FunctionType(
            functionSymbol.Parameters.Select(p => p.Type).ToImmutableArray(),
            functionSymbol.ReturnType);
        var argumentTypes = new[] { left.TypeRequired, right.TypeRequired };
        return this.solver
            .Call(methodType, argumentTypes)
            .ConfigureDiagnostic(diag => diag
                // TODO: This is a horrible way to set the reference...
                // We should definitely rework the location API...
                .WithLocation(new Internal.Diagnostics.Location.TreeReference(syntax)))
            .Result;
    }

    /// <summary>
    /// Constraints that a comparison operator is being invoked.
    /// </summary>
    /// <param name="operator">The operator symbol.</param>
    /// <param name="left">The left operand.</param>
    /// <param name="right">The right operand.</param>
    /// <param name="syntax">The syntax invoking the operator.</param>
    /// <returns>The result of the comparison, the boolean type.</returns>
    public Type CallComparisonOperator(Symbol @operator, UntypedExpression left, UntypedExpression right, ComparisonElementSyntax syntax) =>
        throw new System.NotImplementedException();
}
