using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

public abstract class EndToEndTestsBase
{
    protected static Assembly Compile(string sourceCode)
    {
        var syntaxTree = SyntaxTree.Parse(sourceCode);
        var compilation = Compilation.Create(syntaxTree);

        using var peStream = new MemoryStream();
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
            .GetType("FreeFunctions")?
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args);
        return result!;
    }
}
