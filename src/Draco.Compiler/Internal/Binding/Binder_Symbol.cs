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
using Draco.Compiler.Internal.Symbols.Script;
using Draco.Compiler.Internal.Symbols.Source;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

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

    public virtual ScriptBinding BindScript(ScriptModuleSymbol module, DiagnosticBag diagnostics)
    {
        // Binding scripts is a little different, since they share the inference context,
        // meaning that a global can be inferred from a much later context

        var solver = new ConstraintSolver(module.DeclaringSyntax, "script");

        var globalBindings = ImmutableDictionary.CreateBuilder<VariableDeclarationSyntax, GlobalBinding>();
        var functionBodies = ImmutableDictionary.CreateBuilder<FunctionDeclarationSyntax, BoundStatement>();
        var evalFuncStatements = ImmutableArray.CreateBuilder<BoundStatement>();

        // NOTE: Since the API is asynchronous where task results are only available after
        // the solver has been asked to solve, we store "continuations" here and run them at the end
        // Quite hacky, but will work for now
        var fillerTasks = new List<Action>();

        // Go through all statements and bind them
        foreach (var stmt in module.DeclaringSyntax.Statements)
        {
            if (stmt is DeclarationStatementSyntax declStmt)
            {
                var decl = declStmt.Declaration;
                // Imports are skipped
                if (decl is ImportDeclarationSyntax) continue;
                // Globals mean an assignment into the eval function
                if (decl is VariableDeclarationSyntax varDecl)
                {
                    // Retrieve the symbol
                    var symbol = module.Members
                        .OfType<ScriptGlobalSymbol>()
                        .First(g => g.DeclaringSyntax == varDecl);

                    BindGlobal(symbol);

                    continue;
                }
                // Functions are just bound in this context
                if (decl is FunctionDeclarationSyntax funcDecl)
                {
                    // Retrieve the symbol
                    var symbol = module.Members
                        .OfType<ScriptFunctionSymbol>()
                        .First(f => f.DeclaringSyntax == funcDecl);

                    BindFunction(symbol);

                    continue;
                }
            }
            else
            {
                // Regular statement, that goes into the eval function
                var evalFuncStmt = this.BindStatement(stmt, solver, diagnostics);
                fillerTasks.Add(() => evalFuncStatements.Add(evalFuncStmt.Result));
            }
        }

        // Infer evaluation type
        var evalType = WellKnownTypes.Unit;
        if (module.DeclaringSyntax.Value is not null)
        {
            // Bind the expression
            var resultValue = this.BindExpression(module.DeclaringSyntax.Value, solver, diagnostics);
            evalType = resultValue.GetResultType(module.DeclaringSyntax.Value, solver, diagnostics);
            // Add return statement
            fillerTasks.Add(() => evalFuncStatements.Add(ExpressionStatement(ReturnExpression(resultValue.Result))));
        }
        else
        {
            // Add a default return statement
            fillerTasks.Add(() => evalFuncStatements.Add(ExpressionStatement(ReturnExpression(BoundUnitExpression.Default))));
        }

        // Run the solver
        solver.Solve(diagnostics);

        // Now we can run the fillers
        foreach (var filler in fillerTasks) filler();

        // And finally have all the results
        return new ScriptBinding(
            GlobalBindings: globalBindings.ToImmutable(),
            FunctionBodies: functionBodies.ToImmutable(),
            EvalBody: ExpressionStatement(BlockExpression(
                locals: [],
                statements: evalFuncStatements.ToImmutableArray(),
                value: BoundUnitExpression.Default)),
            EvalType: evalType);

        void BindGlobal(ScriptGlobalSymbol symbol)
        {
            var typeSyntax = symbol.DeclaringSyntax.Type;
            var valueSyntax = symbol.DeclaringSyntax.Value;

            var type = typeSyntax is null ? null : this.BindTypeToTypeSymbol(typeSyntax.Type, diagnostics);
            var valueTask = valueSyntax is null ? null : this.BindExpression(valueSyntax.Value, solver, diagnostics);

            // Infer declared type
            var declaredType = type ?? solver.AllocateTypeVariable();

            // Unify with the type declared on the symbol
            ConstraintSolver.UnifyAsserted(declaredType, symbol.Type);

            // Add assignability constraint, if needed
            if (valueTask is not null)
            {
                solver.Assignable(
                    declaredType,
                    valueTask.GetResultType(valueSyntax!.Value, solver, diagnostics),
                    valueSyntax.Value);
            }

            fillerTasks.Add(() =>
            {
                var assignedValue = valueTask?.Result;
                if (assignedValue is not null)
                {
                    // Add the assignment to the eval function
                    evalFuncStatements.Add(ExpressionStatement(AssignmentExpression(
                        compoundOperator: null,
                        left: GlobalLvalue(symbol),
                        right: assignedValue)));
                }

                globalBindings.Add(symbol.DeclaringSyntax, new(declaredType, assignedValue));
            });
        }

        void BindFunction(ScriptFunctionSymbol symbol)
        {
            var functionName = symbol.DeclaringSyntax.Name.Text;
            var constraints = new ConstraintSolver(symbol.DeclaringSyntax, $"function {functionName}");
            var statementTask = this.BindStatement(symbol.DeclaringSyntax.Body, constraints, diagnostics);

            fillerTasks.Add(() => functionBodies.Add(symbol.DeclaringSyntax, statementTask.Result));
        }
    }
}
