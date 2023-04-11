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

        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        using var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream: peStream);

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
            .GetType("FreeFunctions")?
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args);
        return result!;
    }
}
