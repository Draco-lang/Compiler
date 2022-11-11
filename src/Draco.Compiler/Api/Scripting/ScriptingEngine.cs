using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
    public static void Execute(Draco.Compiler.Api.Compilation compilation)
    {
        GenerateExe(compilation);
        // TODO: If GenerateExe fails, we shouldn't continue
        // Dump runtime config
        File.WriteAllText(
            $"{Path.GetFileNameWithoutExtension(compilation.CompiledExecutablePath!.Name)}.runtimeconfig.json",
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
            Arguments = $"exec {compilation.CompiledExecutablePath}",
        };
        var process = Process.Start(startInfo) ?? throw new InvalidOperationException();
        process.WaitForExit();
        Console.WriteLine($"Process terminated with exit code: {process.ExitCode}");
    }

    public static void GenerateExe(Draco.Compiler.Api.Compilation compilation)
    {
        if (compilation.CompiledExecutablePath is null) throw new InvalidOperationException("Path for the compiled executable was not specified, so the code can't be compiled");
        using (Stream dllStream = new FileStream(compilation.CompiledExecutablePath.FullName, FileMode.OpenOrCreate))
        {
            if (!CompileToAssembly(compilation, dllStream)) return;
        }
    }

    /// <summary>
    /// Works in browsers.
    /// </summary>
    public static void InlineExecute(Compilation compilation)
    {
        using var memStrem = new MemoryStream();
        if (!CompileToAssembly(compilation, memStrem, (config) => config.WithConcurrentBuild(false))) return;
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

    public static bool CompileToAssembly(Draco.Compiler.Api.Compilation dracoCompilation, Stream stream,
        Func<CSharpCompilationOptions, CSharpCompilationOptions>? csCompilerOptionBuilder = null
    )
    {
        CompileToCSharpCode(dracoCompilation);
        // NOTE: This is temporary, we shouldn't rely on compiling to C#
        // and then letting Roslyn do the work

        var options = new CSharpCompilationOptions(OutputKind.ConsoleApplication);
        if (csCompilerOptionBuilder is not null) options = csCompilerOptionBuilder(options);

        // Compile
        var cSharpCompilation = CSharpCompilation.Create(
            assemblyName: dracoCompilation.CompiledExecutablePath!.Name,
            syntaxTrees: new[] { CSharpSyntaxTree.ParseText(dracoCompilation.GeneratedCSharp!) },
            references: ReferenceAssemblies.Net60,
            options: options);
        {
            var emitResult = cSharpCompilation.Emit(stream);
            // See if we succeeded
            if (emitResult.Success) return true;
            Console.WriteLine("Failed to compile transpiled C# code");
            foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
            Console.WriteLine("====================================");
            Console.WriteLine(dracoCompilation.GeneratedCSharp!);
            return false;
        }
    }

    public static void CompileToCSharpCode(Draco.Compiler.Api.Compilation compilation)
    {
        compilation.Parsed = ParseTree.Parse(compilation.Source);
        compilation.GeneratedCSharp = CSharpCodegen.Transpile(compilation.Parsed.Green);
    }

    public static void CompileToParseTree(Draco.Compiler.Api.Compilation compilation)
    {
        compilation.Parsed = ParseTree.Parse(compilation.Source);
    }
}
