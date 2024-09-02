using System;
using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;

/// <summary>
/// A code generator that compiles only the minimal number of procedures in order to evaluate something.
/// Used for compile-time execution.
/// </summary>
internal sealed class MinimalAssemblyCodegen(Compilation compilation)
{
    private Module Module => this.assembly.RootModule;

    private readonly Assembly assembly = new(TODO_SYMBOL);

    private readonly Dictionary<FunctionSymbol, Procedure> compiledProcedures = [];
    private readonly Queue<FunctionSymbol> functionsToCompile = [];

    private void CompileAllFunctions()
    {
        while (this.functionsToCompile.TryDequeue(out var function))
        {
            if (this.compiledProcedures.ContainsKey(function)) continue;

            var procedure = this.CompileFunction(function);
            if (procedure is not null)
            {
                this.compiledProcedures.Add(function, procedure);
            }
        }
    }

    private Procedure? CompileFunction(FunctionSymbol function)
    {
        if (function.Body is null) return null;

        // Create a procedure we compile into
        var procedure = new Procedure(this.Module, function);

        // TODO: Little copypasta from ModuleCodegen, maybe refactor
        // Create the body
        var body = this.RewriteBody(function.Body);
        // Yank out potential local functions and closures
        var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
        // Compile it
        var bodyCodegen = new LocalCodegen(compilation, procedure);
        bodyWithoutLocalFunctions.Accept(bodyCodegen);

        // Add the rest to the worklist
        foreach (var localFunction in localFunctions)
        {
            this.functionsToCompile.Enqueue(localFunction);
        }

        return procedure;
    }

    // NOTE: For now we don't inject sequence points
    private BoundNode RewriteBody(BoundNode body) => body.Accept(new LocalRewriter(compilation));
}
