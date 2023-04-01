using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public static Assembly Generate(ModuleSymbol symbol)
    {
        var codegen = new ModuleCodegen(symbol);
        symbol.Accept(codegen);
        codegen.Complete();
        return codegen.assembly;
    }

    private readonly Assembly assembly;
    private readonly FunctionBodyCodegen globalInitializer;

    private ModuleCodegen(ModuleSymbol module)
    {
        this.assembly = new(module);
        this.globalInitializer = new(this.assembly.GlobalInitializer);
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
            // Desugar value
            var body = sourceGlobal.Value.Accept(LocalRewriter.Instance);
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

        // TODO: Return type

        // Generate function body
        var bodyCodegen = new FunctionBodyCodegen(procedure);
        // Desugar it
        var body = sourceFunction.Body.Accept(LocalRewriter.Instance);
        body.Accept(bodyCodegen);
    }
}
