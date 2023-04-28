using System.Collections.Immutable;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

public abstract class EndToEndTestsBase
{
    protected static Assembly Compile(string sourceCode)
    {
        var syntaxTree = SyntaxTree.Parse(sourceCode);
        return Compile(null, syntaxTree);
    }

    protected static Assembly Compile(string? root, params SyntaxTree[] trees)
    {
        var compilation = Compilation.Create(
            syntaxTrees: trees.ToImmutableArray(),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModule: root);

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream: peStream);

        Assert.True(emitResult.Success);

        // Load emitted bytes as assembly
        peStream.Position = 0;
        var peBytes = peStream.ToArray();
        var assembly = Assembly.Load(peBytes);

        return assembly;
    }

    protected static TResult Invoke<TResult>(
        Assembly assembly,
        string methodName,
        TextReader? stdin,
        TextWriter? stdout,
        params object[] args)
    {
        Console.SetIn(stdin ?? Console.In);
        Console.SetOut(stdout ?? Console.Out);

        var method = assembly
            .GetType("FreeFunctions")?
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args);
        return result!;
    }

    protected static TResult Invoke<TResult>(Assembly assembly, string methodName, params object[] args) => Invoke<TResult>(
        assembly: assembly,
        methodName: methodName,
        stdin: null,
        stdout: null,
        args: args);
}
