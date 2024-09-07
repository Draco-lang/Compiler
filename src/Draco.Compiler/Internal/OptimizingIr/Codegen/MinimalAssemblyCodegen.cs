using System.Collections.Generic;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.OptimizingIr.Codegen;

/// <summary>
/// A code generator that compiles only the minimal number of procedures in order to evaluate something.
/// Used for compile-time execution.
/// </summary>
internal sealed class MinimalAssemblyCodegen(Compilation compilation)
{
    /// <summary>
    /// The assembly containing the compiled procedures.
    /// </summary>
    public Assembly Assembly { get; } = new(new MinimalModuleSymbol(CompilerConstants.CompileTimeModuleName));

    /// <summary>
    /// The main module of the assembly.
    /// </summary>
    public Module Module => this.Assembly.RootModule;

    private readonly Dictionary<FunctionSymbol, Procedure> compiledProcedures = [];
    private readonly Queue<FunctionSymbol> functionsToCompile = [];

    public void Compile(FunctionSymbol function)
    {
        this.functionsToCompile.Enqueue(function);
        this.CompileAllFunctions();
        this.CompleteGlobalInitializerIfneeded();
    }

    private void CompleteGlobalInitializerIfneeded()
    {
        var firstBasicBlock = this.Module.GlobalInitializer.BasicBlocks.Values.First();
        if (firstBasicBlock.Instructions.Any()) return;

        var globalInitializer = new LocalCodegen(compilation, this.Module.GlobalInitializer);
        globalInitializer.Write(Ret(default(Void)));
    }

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
        var procedure = this.Module.DefineProcedure(function);

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

        // Add referenced functions to the worklist
        foreach (var referencedFunction in procedure.GetReferencedFunctions())
        {
            this.functionsToCompile.Enqueue(referencedFunction);
        }

        return procedure;
    }

    // NOTE: For now we don't inject sequence points
    private BoundNode RewriteBody(BoundNode body) => body.Accept(new LocalRewriter(compilation));
}
