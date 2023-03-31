using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.Lowering;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Source;

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
        return codegen.assembly;
    }

    private readonly Assembly assembly;

    private ModuleCodegen(ModuleSymbol module)
    {
        this.assembly = new(module);
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
