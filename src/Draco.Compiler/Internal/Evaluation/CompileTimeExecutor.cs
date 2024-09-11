using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.OptimizingIr.Codegen;
using Draco.Compiler.Internal.Solver;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Evaluation;

/// <summary>
/// Implements compile-time evaluation and execution.
/// </summary>
internal sealed class CompileTimeExecutor(Compilation compilation)
{
    /// <summary>
    /// The root module of the generated assembly.
    /// </summary>
    public OptimizingIr.Model.Module RootModule => this.codegen.Module;

    private readonly MinimalAssemblyCodegen codegen = new(compilation);
    private readonly MemoryStream peStream = new();
    private readonly AssemblyLoadContext assemblyLoadContext = new("compile-time evaluator", isCollectible: true);

    private int evalCount;

    /// <summary>
    /// Evaluates the given syntax node.
    /// </summary>
    /// <param name="syntax">The syntax node to evaluate.</param>
    /// <returns>The result of the evaluation.</returns>
    public object? Evaluate(SyntaxNode syntax)
    {
        if (syntax is not ExpressionSyntax expression)
        {
            throw new NotSupportedException($"the evaluation of {syntax.GetType().Name} is not supported");
        }

        // Bind and wrap into a function
        var boundExpression = this.BindExpression(expression);
        var evalFunction = this.CreateEvalFunction(boundExpression);

        // Generate necessary code
        this.codegen.Compile(evalFunction);
        var assembly = this.codegen.Assembly;

        // Reset the stream, emit CIL
        this.peStream.Position = 0;
        MetadataCodegen.Generate(compilation, assembly, this.peStream, null, flags: CodegenFlags.RedirectHandlesToRoot);

        // Load the assembly and execute the function
        this.peStream.Position = 0;
        var loadedAssembly = this.assemblyLoadContext.LoadFromStream(this.peStream);
        if (loadedAssembly is null) throw new InvalidOperationException("failed to load the assembly");

        var mainModule = loadedAssembly.GetType(CompilerConstants.CompileTimeModuleName);
        if (mainModule is null) throw new InvalidOperationException("failed to load the main module");

        var evalMethod = mainModule.GetMethod(evalFunction.Name, BindingFlags.NonPublic | BindingFlags.Static);
        if (evalMethod is null) throw new InvalidOperationException("failed to load the eval method");

        return evalMethod.Invoke(null, null);
    }

    private FunctionSymbol CreateEvalFunction(BoundExpression expression) => new IntrinsicFunctionSymbol(
        name: $".eval{this.evalCount++}",
        paramTypes: [],
        returnType: expression.TypeRequired,
        body: ExpressionStatement(ReturnExpression(expression)));

    private BoundExpression BindExpression(ExpressionSyntax syntax)
    {
        var binder = compilation.GetBinder(syntax);
        var solver = new ConstraintSolver(binder, "compile-time evaluation");
        var bindingTask = binder.BindExpression(syntax, solver, compilation.GlobalDiagnosticBag);
        solver.Solve(compilation.GlobalDiagnosticBag);
        return bindingTask.Result;
    }
}
