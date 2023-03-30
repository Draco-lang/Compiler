using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

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

    public ModuleCodegen(ModuleSymbol module)
    {
        this.assembly = new(module);
    }
}
