using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Diagnostics;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.Binding;

internal partial class Binder
{
    public virtual BoundStatement BindFunction(SourceFunctionSymbol function, DiagnosticBag diagnostics)
    {
        var functionName = function.DeclaringSyntax.Name.Text;
        var constraints = new ConstraintSolver(function.DeclaringSyntax, $"function {functionName}");
        var statementTask = this.BindStatement(function.DeclaringSyntax.Body, constraints, diagnostics);
        constraints.Solve(diagnostics);
        return statementTask.Result;
    }

    public virtual GlobalBinding BindGlobal(SourceGlobalSymbol global, DiagnosticBag diagnostics)
    {
        var globalName = global.DeclaringSyntax.Name.Text;
        var constraints = new ConstraintSolver(global.DeclaringSyntax, $"global {globalName}");

        var typeSyntax = global.DeclaringSyntax.Type;
        var valueSyntax = global.DeclaringSyntax.Value;

        // Bind type and value
        var type = typeSyntax is null ? null : this.BindTypeToTypeSymbol(typeSyntax.Type, diagnostics);
        var valueTask = valueSyntax is null
            ? null
            : this.BindExpression(valueSyntax.Value, constraints, diagnostics);

        // Infer declared type
        var declaredType = type ?? constraints.AllocateTypeVariable(track: false);

        // Add assignability constraint, if needed
        if (valueTask is not null)
        {
            constraints.Assignable(
                declaredType,
                valueTask.GetResultType(valueSyntax, constraints, diagnostics),
                global.DeclaringSyntax.Value!.Value);
        }

        // Solve
        constraints.Solve(diagnostics);

        // Type out the expression, if needed
        var boundValue = valueTask?.Result;

        // Unwrap the type
        declaredType = declaredType.Substitution;

        if (declaredType.IsTypeVariable)
        {
            // We could not infer the type
            diagnostics.Add(Diagnostic.Create(
                template: TypeCheckingErrors.CouldNotInferType,
                location: global.DeclaringSyntax.Location,
                formatArgs: global.Name));
            // We use an error type
            declaredType = WellKnownTypes.ErrorType;
        }

        // Done
        return new(declaredType, boundValue);
    }
}
