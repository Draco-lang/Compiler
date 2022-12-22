using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Compiler.Internal.Utilities;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.Compiler.Tests.EndToEnd;

public abstract class EndToEndTestsBase
{
    protected static Assembly Compile(string sourceCode)
    {
        // TODO: We temporarily inject a main method
        sourceCode = $$"""
            func main() {}
            {{sourceCode}}
            """;

        var parseTree = ParseTree.Parse(sourceCode);
        var compilation = Compilation.Create(parseTree);

        using var peStream = new MemoryStream();
        var options = new CSharpCompilationOptions(Microsoft.CodeAnalysis.OutputKind.DynamicallyLinkedLibrary);
        var emitResult = compilation.Emit(peStream);

        Assert.True(emitResult.Success);

        // Load emitted bytes as assembly
        peStream.Position = 0;
        var peBytes = peStream.ToArray();
        var assembly = Assembly.Load(peBytes);

        return assembly;
    }

    protected static TResult Invoke<TResult>(Assembly assembly, string methodName, params object[] args)
    {
        var method = assembly
            .GetType("DracoProgram")?
            .GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args);
        return result!;
    }
}
