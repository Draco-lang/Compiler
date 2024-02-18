using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Xunit.Abstractions;

namespace Draco.Debugger.Tests;

public sealed class TestDebugSession
{
    public Debugger Debugger { get; }
    public SourceFile File { get; }
    public string DllLocation { get; }
    public string PdbLocation { get; }
    public string RuntimeConfigLocation { get; }
    private static IEnumerable<MetadataReference> BclReferences => Net80.ReferenceInfos.All
       .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    private TestDebugSession(Debugger debugger, SourceFile file, string dllLocation, string pdbLocation, string runtimeConfigLocation)
    {
        this.Debugger = debugger;
        this.File = file;
        this.DllLocation = dllLocation;
        this.PdbLocation = pdbLocation;
        this.RuntimeConfigLocation = runtimeConfigLocation;
    }

    public static async Task<TestDebugSession> DebugAsync(string code, ITestOutputHelper output, [CallerMemberName] string? testName = null)
    {
        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        {
            throw new Xunit.SkipException("Debugger only works on windows now");
        }

        var host = DebuggerHost.Create();
        ArgumentNullException.ThrowIfNull(testName);
        var path = $"{testName}-{Guid.NewGuid()}";
        var mainPath = $"{path}.draco";
        System.IO.File.WriteAllText(mainPath, code);
        var sourceText = SourceText.FromFile(Path.GetFullPath(mainPath));
        var syntaxTree = SyntaxTree.Parse(sourceText);
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            metadataReferences: BclReferences.ToImmutableArray(),
            assemblyName: path
        );
        var dllLocation = $"{path}.dll";
        var pdbLocation = $"{path}.pdb";
        using (var peStream = new FileStream(dllLocation, FileMode.Create))
        using (var pdbStream = new FileStream(pdbLocation, FileMode.Create))
        {
            var emitResult = compilation.Emit(peStream: peStream, pdbStream: pdbStream);
            if (!emitResult.Success) throw new InvalidOperationException($"Invalid code {string.Join('\n', emitResult.Diagnostics)}. ");
        }

        var runtimeConfigLocation = $"{path}.runtimeconfig.json";
        System.IO.File.WriteAllText(runtimeConfigLocation, """
            {
              "runtimeOptions": {
                "tfm": "net8.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "8.0.0"
                }
              }
            }
            """);

        var debugger = host.StartProcess("dotnet", dllLocation);
        debugger.OnEventLog += (s, e) => output.WriteLine(e);
        await debugger.Ready;
        return new TestDebugSession(debugger, debugger.MainModule.SourceFiles.Single(), dllLocation, pdbLocation, runtimeConfigLocation);
    }
}
