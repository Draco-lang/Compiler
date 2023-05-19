using System.Collections.Immutable;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Internal;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests.EndToEnd;

public abstract class EndToEndTestsBase
{
    protected static Assembly Compile(string sourceCode)
    {
        var syntaxTree = SyntaxTree.Parse(sourceCode);
        return Compile(null, syntaxTree);
    }

    protected static Assembly Compile(string? root, params SyntaxTree[] syntaxTrees) =>
        Compile(root, syntaxTrees.ToImmutableArray(), ImmutableArray<MetadataReference>.Empty);

    protected static Assembly Compile(
        string? root,
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<MetadataReference> additionalMetadataReferences)
    {
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Concat(additionalMetadataReferences)
                .ToImmutableArray(),
            rootModulePath: root);

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
        string moduleName = CompilerConstants.DefaultModuleName,
        params object[] args)
    {
        Console.SetIn(stdin ?? Console.In);
        Console.SetOut(stdout ?? Console.Out);

        // NOTE: nested typed are not separated by . but by + in IL, thats the reason for the replace
        var method = assembly
            .GetType(moduleName.Replace('.', '+'))?
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(method);

        var result = (TResult?)method?.Invoke(null, args);
        return result!;
    }

    protected static TResult Invoke<TResult>(Assembly assembly, string moduleName, string methodName, params object[] args) => Invoke<TResult>(
        assembly: assembly,
        moduleName: moduleName,
        methodName: methodName,
        stdin: null,
        stdout: null,
        args: args);

    protected static TResult Invoke<TResult>(Assembly assembly, string methodName, params object[] args) => Invoke<TResult>(
        assembly: assembly,
        moduleName: CompilerConstants.DefaultModuleName,
        methodName: methodName,
        stdin: null,
        stdout: null,
        args: args);
}
