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
/// Generates metadata.
/// </summary>
internal sealed class MetadataCodegen
{
    // TODO: Doc
    public static void Generate(Assembly assembly, Stream peStream) =>
        throw new NotImplementedException();

    public EntityHandle GetGlobalHandle(Global global) => throw new NotImplementedException();
    public EntityHandle GetProcedureHandle(IProcedure procedure) => throw new NotImplementedException();
    public UserStringHandle GetStringLiteralHandle(string text) => throw new NotImplementedException();
}
