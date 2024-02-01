using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;

namespace Draco.Debugger.Tests;
static class TextDebuggerHelper
{

    public static string FindDbgShim()
    {
        var root = "C:\\Program Files\\dotnet\\shared\\Microsoft.NETCore.App";

        if (!Directory.Exists(root))
        {
            throw new InvalidOperationException($"Cannot find dbgshim.dll: '{root}' does not exist");
        }

        foreach (var dir in Directory.EnumerateDirectories(root).Reverse())
        {
            var dbgshim = Directory.EnumerateFiles(dir, "dbgshim.dll").FirstOrDefault();
            if (dbgshim is not null) return dbgshim;
        }

        throw new InvalidOperationException($"Failed to find a runtime containing dbgshim.dll under '{root}'");
    }
    private static IEnumerable<MetadataReference> BclReferences => Net70.ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    public static async Task<(Debugger, SourceFile)> DebugAsync(this DebuggerHost host, string code, [CallerMemberName] string? testName = null)
    {
        if (testName is null) throw new ArgumentNullException(nameof(testName));
        var mainPath = $"{testName}.draco";
        File.WriteAllText(mainPath, code);
        var sourceText = SourceText.FromFile(Path.GetFullPath(mainPath));
        var syntaxTree = SyntaxTree.Parse(sourceText);
        var compilation = Compilation.Create(
            syntaxTrees: [syntaxTree],
            metadataReferences: BclReferences.ToImmutableArray()
        );
        var dllLocation = $"{testName}.dll";
        var pdbLocation = $"{testName}.pdb";
        using (var peStream = new FileStream(dllLocation, FileMode.Create))
        using (var pdbStream = new FileStream(pdbLocation, FileMode.Create))
        {
            var emitResult = compilation.Emit(peStream: peStream, pdbStream: pdbStream);
            if (!emitResult.Success) throw new InvalidOperationException($"Invalid code {string.Join('\n', emitResult.Diagnostics)}. ");
        }
        File.WriteAllText($"{testName}.runtimeconfig.json", """
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
        await debugger.Ready;
        return (debugger, debugger.MainModule.SourceFiles.Single());
    }
}
