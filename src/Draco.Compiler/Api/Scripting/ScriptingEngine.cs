using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;

namespace Draco.Compiler.Api.Scripting;

public static class ScriptingEngine
{
    public static object? Execute(Compilation compilation)
    {
        using var peStream = new MemoryStream();
        compilation.Emit(
            peStream,
            csCompilerOptionBuilder: opt => new Microsoft.CodeAnalysis.CSharp.CSharpCompilationOptions(
                Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary));
        peStream.Position = 0;
        var peBytes = peStream.ToArray();

        var assembly = Assembly.Load(peBytes);

        var mainMethod = assembly
            .GetType("Program")?
            .GetMethod("Main");
        return mainMethod?.Invoke(null, new[] { Array.Empty<string>() });
    }
}
