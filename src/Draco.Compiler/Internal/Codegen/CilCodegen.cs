using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Draco.Compiler.Internal.OptimizingIr.Model;
// using Draco.Compiler.Internal.DracoIr;
using Draco.Compiler.Internal.Symbols.Synthetized;
// using Type = Draco.Compiler.Internal.DracoIr.Type;

namespace Draco.Compiler.Internal.Codegen;

/// <summary>
/// Generates CIL from IR.
/// </summary>
internal sealed class CilCodegen
{
    // TODO: Doc
    public static void Generate(Assembly assembly, Stream peStream) =>
        throw new NotImplementedException();
}
