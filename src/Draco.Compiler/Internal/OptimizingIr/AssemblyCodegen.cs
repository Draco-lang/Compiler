using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.OptimizingIr;

/// <summary>
/// Generates IR code on top-level.
/// </summary>
internal sealed partial class AssemblyCodegen
{
    /// <summary>
    /// Generates IR of this <see cref="Compilation"/>.
    /// </summary>
    /// <param name="compilation">The compilation to generate IR for.</param>
    /// <param name="emitSequencePoints">If true, sequence points will be generated in the IR.</param>
    /// <returns>Generated IR.</returns>
    public static Assembly Generate(
        Compilation compilation,
        bool emitSequencePoints)
    {
        var assemblyCodegen = new AssemblyCodegen(compilation);
        var moduleCodegen = new ModuleCodegen(compilation, assemblyCodegen.assembly.RootModule, emitSequencePoints);
        compilation.SourceModule.Accept(moduleCodegen);
        assemblyCodegen.Complete();
        return assemblyCodegen.assembly;
    }

    private readonly Assembly assembly;

    private AssemblyCodegen(Compilation compilation)
    {
        this.assembly = new(compilation.SourceModule)
        {
            Name = compilation.AssemblyName,
        };
    }

    private void Complete()
    {
        // Set the entry point, in case we have one
        var mainProcedure = (Procedure?)this.assembly.RootModule.Procedures.Values
            .FirstOrDefault(p => p.Name == CompilerConstants.EntryPointName);
        this.assembly.EntryPoint = mainProcedure;
    }
}
