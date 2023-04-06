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
            var body = sourceGlobal.Value;
            // If needed, inject sequence points
            if (this.emitSequencePoints) body = (BoundExpression)body.Accept(SequencePointInjector.Instance);
            // Desugar value
            body = (BoundExpression)body.Accept(LocalRewriter.Instance);
            // Compile it
            var value = body.Accept(this.globalInitializer);
            // Store it
            this.globalInitializer.Write(Store(global, value));
        }
    }

    public override void VisitFunction(FunctionSymbol functionSymbol)
    {
        if (functionSymbol is not SourceFunctionSymbol sourceFunction) return;

        var procedure = this.assembly.DefineProcedure(functionSymbol);

        // Define parameters
        foreach (var param in functionSymbol.Parameters) procedure.DefineParameter(param);

        // Generate function body
        var bodyCodegen = new FunctionBodyCodegen(procedure);
        var body = sourceFunction.Body;
        // If needed, inject sequence points
        if (this.emitSequencePoints) body = (BoundStatement)body.Accept(SequencePointInjector.Instance);
        // Desugar it
        body = (BoundStatement)body.Accept(LocalRewriter.Instance);
        // Compile it
        body.Accept(bodyCodegen);
    }
}
