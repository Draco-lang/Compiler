using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Internal.OptimizingIr.InstructionFactory;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code on module-level.
/// </summary>
internal sealed class ModuleCodegen : SymbolVisitor
{
    public static Assembly Generate(
        Compilation compilation,
        ModuleSymbol symbol,
        bool emitSequencePoints)
    {
        var codegen = new ModuleCodegen(compilation, symbol, emitSequencePoints);
        symbol.Accept(codegen);
        codegen.Complete();
        return codegen.assembly;
    }

    private readonly Assembly assembly;
    private readonly FunctionBodyCodegen globalInitializer;
    private readonly bool emitSequencePoints;

    private ModuleCodegen(Compilation compilation, ModuleSymbol module, bool emitSequencePoints)
    {
        this.assembly = new(module)
        {
            Name = compilation.AssemblyName,
        };
        this.globalInitializer = new(this.assembly.GlobalInitializer);
        this.emitSequencePoints = emitSequencePoints;
    }

    private void Complete()
    {
        // Complete anything that needs completion
        // The global initializer for example is missing a return
        this.globalInitializer.Write(Ret(default(Void)));

        // We can also set the entry point, in case we have one
        var mainProcedure = (Procedure?)this.assembly.Procedures.Values
            .FirstOrDefault(p => p.Name == "main");
        this.assembly.EntryPoint = mainProcedure;
    }

    public override void VisitGlobal(GlobalSymbol globalSymbol)
    {
        if (globalSymbol is not SourceGlobalSymbol sourceGlobal) return;

        var global = this.assembly.DefineGlobal(sourceGlobal);

        // If there's a value, compile it
        if (sourceGlobal.Value is not null)
        {
            var body = this.RewriteBody(sourceGlobal.Value);
            // Yank out potential local functions and closures
            var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
            // Compile it
            var value = bodyWithoutLocalFunctions.Accept(this.globalInitializer);
            // Store it
            this.globalInitializer.Write(Store(global, value));

            // Compile the local functions
            foreach (var localFunc in localFunctions)
            {
                if (localFunc is not SourceFunctionSymbol sourceLocalFunc) continue;

                var localBody = (BoundStatement)this.RewriteBody(sourceLocalFunc.Body);
                this.CompileFunctionWithBody(sourceLocalFunc, localBody);
            }
        }
    }

    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol is not SourceFunctionSymbol sourceFunction) return;

        var body = this.RewriteBody(sourceFunction.Body);
        // Yank out potential local functions and closures
        var (bodyWithoutLocalFunctions, localFunctions) = ClosureRewriter.Rewrite(body);
        // Compile it
        this.CompileFunctionWithBody(sourceFunction, (BoundStatement)bodyWithoutLocalFunctions);

        // Compile the local functions
        foreach (var localFunc in localFunctions)
        {
            if (localFunc is not SourceFunctionSymbol sourceLocalFunc) continue;

            var localBody = (BoundStatement)this.RewriteBody(sourceLocalFunc.Body);
            this.CompileFunctionWithBody(sourceLocalFunc, localBody);
        }
    }

    private void CompileFunctionWithBody(FunctionSymbol functionSymbol, BoundStatement body)
    {
        var procedure = this.assembly.DefineProcedure(functionSymbol);

        // Define parameters
        foreach (var param in functionSymbol.Parameters) procedure.DefineParameter(param);

        // Transform body
        body = (BoundStatement)this.RewriteBody(body);
        // Compile it
        var bodyCodegen = new FunctionBodyCodegen(procedure);
        body.Accept(bodyCodegen);
    }

    private BoundNode RewriteBody(BoundNode body)
    {
        // If needed, inject sequence points
        if (this.emitSequencePoints) body = body.Accept(SequencePointInjector.Instance);
        // Desugar it
        body = body.Accept(LocalRewriter.Instance);
        // Done
        return body;
    }
}
