using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.Loader;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal;

namespace Draco.Compiler.Tests.EndToEnd;

public abstract class EndToEndTestsBase
{
    protected static Assembly Compile(string sourceCode)
    {
        var syntaxTree = SyntaxTree.Parse(sourceCode);
        return Compile(null, syntaxTree);
    }

    protected static Assembly Compile(string? root, params SyntaxTree[] syntaxTrees) =>
        Compile(root, syntaxTrees.ToImmutableArray(), ImmutableArray<(string Name, Stream Stream)>.Empty);

    protected static Assembly Compile(
        string? root,
        ImmutableArray<SyntaxTree> syntaxTrees,
        ImmutableArray<(string Name, Stream Stream)> additionalPeReferences)
    {
        using var peStream = CompileRaw(root, syntaxTrees, additionalPeReferences);
        return LoadAssembly(additionalPeReferences, peStream);
    }

    protected static MemoryStream CompileRaw(string sourceCode)
    {
        var syntaxTree = SyntaxTree.Parse(sourceCode);
        return CompileRaw(null, ImmutableArray.Create(syntaxTree), ImmutableArray<(string, Stream)>.Empty);
    }

    private static MemoryStream CompileRaw(string? root, ImmutableArray<SyntaxTree> syntaxTrees, ImmutableArray<(string Name, Stream Stream)> additionalPeReferences)
    {
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Concat(additionalPeReferences.Select(r =>
                {
                    var streamCopy = new MemoryStream();
                    r.Stream.CopyTo(streamCopy);
                    r.Stream.Position = 0;
                    streamCopy.Position = 0;
                    return MetadataReference.FromPeStream(streamCopy);
                }))
                .ToImmutableArray(),
            rootModulePath: root);

        var peStream = new MemoryStream();
        var emitResult = compilation.Emit(peStream: peStream);
        peStream.Position = 0;

        Assert.True(emitResult.Success);
        return peStream;
    }

    private static Assembly LoadAssembly(ImmutableArray<(string Name, Stream Stream)> additionalPeReferences, MemoryStream peStream)
    {

        // We need a custom load context
        var loadContext = new AssemblyLoadContext("testLoadContext");
        loadContext.Resolving += (loader, name) =>
        {
            // Look through the additional references
            var stream = additionalPeReferences.First(r => r.Name == name.Name).Stream;
            return loader.LoadFromStream(stream);
        };

        // Load emitted bytes as assembly
        var assembly = loadContext.LoadFromStream(peStream);
        return assembly;
    }

    protected static TResult Invoke<TResult>(
        Assembly assembly,
        string methodName,
        TextReader? stdin = null,
        TextWriter? stdout = null,
        string moduleName = Constants.DefaultModuleName,
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
        moduleName: Constants.DefaultModuleName,
        methodName: methodName,
        stdin: null,
        stdout: null,
        args: args);
}
