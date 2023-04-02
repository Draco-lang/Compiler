using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Internal.OptimizingIr.Model;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates PDB from IR.
/// </summary>
internal sealed class PdbCodegen
{
    /// <summary>
    /// GUID for identifying a Draco document.
    /// </summary>
    public static readonly Guid DracoLanguageGuid = new("7ef7b804-0709-43bc-b1b5-998bb801477b");

    // TODO: Doc
    public static void Generate(IAssembly assembly, Stream pdbStream) =>
        throw new NotImplementedException();
}
