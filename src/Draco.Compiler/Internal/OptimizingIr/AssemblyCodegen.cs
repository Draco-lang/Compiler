using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.OptimizingIr.Model;
using Draco.Compiler.Internal.Symbols;

namespace Draco.Compiler.Internal.OptimizingIr;
internal sealed partial class AssemblyCodegen
{
    public static Assembly Generate(
        Compilation compilation,
        ModuleSymbol rootModule,
        bool emitSequencePoints)
    {
        var assemblyCodegen = new AssemblyCodegen(compilation);
        var root = new Module(rootModule, assemblyCodegen.assembly, null);
        assemblyCodegen.assembly.AddRootModule(root);
        var moduleCodegen = new ModuleCodegen(root, compilation, emitSequencePoints);
        rootModule.Accept(moduleCodegen);
        assemblyCodegen.Complete();
        return assemblyCodegen.assembly;
    }

    private readonly Assembly assembly;

    private AssemblyCodegen(Compilation compilation)
    {
        this.assembly = new()
        {
            Name = compilation.AssemblyName,
        };
    }

    private void Complete()
    {
        // Set the entry point, in case we have one
        var mainProcedure = (Procedure?)this.assembly.RootModule.Procedures.Values
            .FirstOrDefault(p => p.Name == "main");
        this.assembly.EntryPoint = mainProcedure;
    }
}
