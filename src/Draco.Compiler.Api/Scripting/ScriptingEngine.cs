using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Basic.Reference.Assemblies;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using static System.Net.Mime.MediaTypeNames;

namespace Draco.Compiler.Api.Scripting;

public static class ScriptingEngine
{
    public static void Execute(string text)
    {
        using (Stream dllStream = new FileStream("transpiledProgram.dll", FileMode.OpenOrCreate))
        {
            if (!CompileToAssembly(text, dllStream)) return;
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

    /// <summary>
    /// Works in browsers.
    /// </summary>
    public static void InlineExecute(string text)
    {
        using var memStrem = new MemoryStream();
        if (!CompileToAssembly(text, memStrem, (config) => config.WithConcurrentBuild(false))) return;
        var assembly = Assembly.Load(memStrem!.ToArray());
        var mainReturnValue = assembly.EntryPoint!.Invoke(null, new object[]
        {
            Array.Empty<string>()
        });
        if (mainReturnValue is Task task)
        {
            task.GetAwaiter().GetResult(); // huuuuh, can we do something else ?
        }
        if (mainReturnValue is Task<int> taskWithRes)
        {
            taskWithRes.GetAwaiter().GetResult();
            mainReturnValue = taskWithRes.Result;
        }
        if (mainReturnValue is int)
        {
            Console.WriteLine($"Process terminated with exit code: {mainReturnValue}");
        }
        else
        {
            Console.WriteLine($"Process terminated.");

        }
        return;
    }

    public static bool CompileToAssembly(string text, Stream stream,
        Func<CSharpCompilationOptions, CSharpCompilationOptions>? csCompilerOptionBuilder = null
    )
    {
        var cSharpCode = CompileToCSharpCode(text);

        // NOTE: This is temporary, we shouldn't rely on compiling to C#
        // and then letting Roslyn do the work

        var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        if (csCompilerOptionBuilder is not null) options = csCompilerOptionBuilder(options);

        // Compile
        var compilation = CSharpCompilation.Create(
            assemblyName: "transpiledProgram",
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(cSharpCode) },
            references: ReferenceAssemblies.Net60,
            options: options);
        {
            EmitResult emitResult;

            emitResult = compilation.Emit(stream);
            // See if we succeeded
            if (emitResult.Success) return true;
            Console.WriteLine("Failed to compile transpiled C# code");
            foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
            Console.WriteLine("====================================");
            Console.WriteLine(cSharpCode);
            return false;
        }
    }

    public static string CompileToCSharpCode(string text)
    {
        var parseTree = ParseTree.Parse(text);
        return CSharpCodegen.Transpile(parseTree.Green);
    }
}
