using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.Compiler.Api.Scripting;

public static class ScriptingEngine
{
    public static void Execute(string text)
    {
        var parseTree = ParseTree.Parse(text);
        var cSharpCode = CSharpCodegen.Transpile(parseTree.Green);

        // NOTE: This is temporary, we shouldn't rely on compiling to C#
        // and then letting Roslyn do the work

        // Compile
        var compilation = CSharpCompilation.Create(
            assemblyName: "transpiledProgram",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(cSharpCode) },
            references: ReferenceAssemblies.Net60,
            options: new(OutputKind.ConsoleApplication));
        {
            using var dllStream = new FileStream("transpiledProgram.dll", FileMode.OpenOrCreate);
            var emitResult = compilation.Emit(dllStream);

            // See if we succeeded
            if (!emitResult.Success)
            {
                Console.WriteLine("Failed to compile transpiled C# code");
                foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
                Console.WriteLine("====================================");
                Console.WriteLine(cSharpCode);
                return;
            }
        }

        // Dump runtime config
        File.WriteAllText(
            "transpiledProgram.runtimeconfig.json",
            $$$"""
            {
              "runtimeOptions": {
                "tfm": "net6.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "6.0.0"
                }
              }
            }
            """);

        // Execute
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"exec transpiledProgram.dll",
        };
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException();
        process.WaitForExit();
        Console.WriteLine($"Process terminated with exit code: {process.ExitCode}");
    }
}

