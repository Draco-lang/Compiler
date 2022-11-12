using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Utilities;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args)
    {
        return ConfigureCommands().Invoke(args);
    }

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo>("file", description: "Draco file");
        var emitCSharpOutput = new Option<FileInfo>("--output-cs", description: "Specifies output file for generated c#, if not specified, generated code is not saved to the disk");
        var outputOption = new Option<FileInfo>(new string[] { "-o", "--output" }, () => new FileInfo("transpiledProgram.exe"), "Specifies the output file");
        var runCommand = new Command("run", "Runs specified draco file")
        {
            fileArgument,
            outputOption
        };
        runCommand.SetHandler((input, output) => Run(input, output), fileArgument, outputOption);

        var generateParseTreeCommand = new Command("parse", "Generates parse tree from specified draco file")
        {
            fileArgument,
        };
        generateParseTreeCommand.SetHandler((file) => GenerateParseTree(file), fileArgument);

        var generateCSCommand = new Command("codegen", "Generates c# from specified draco file and displays it to the console")
        {
            fileArgument,
            emitCSharpOutput,
        };
        generateCSCommand.SetHandler((file, emitCS) => GenerateCSharp(file, emitCS), fileArgument, emitCSharpOutput);

        var generateExeCommand = new Command("compile", "Generates executable from specified draco file")
        {
            fileArgument,
            outputOption
        };
        generateExeCommand.SetHandler((input, output) => GenerateExe(input, output), fileArgument, outputOption);

        var rootCommand = new RootCommand("CLI for the draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateParseTreeCommand);
        rootCommand.AddCommand(generateCSCommand);
        rootCommand.AddCommand(generateExeCommand);
        return rootCommand;
    }

    private static void Run(FileInfo input, FileInfo output)
    {
        var compilation = new Compilation(File.ReadAllText(input.FullName), output.FullName);
        ScriptingEngine.Execute(compilation);
    }

    private static void GenerateParseTree(FileInfo input)
    {
        var compilation = new Compilation(File.ReadAllText(input.FullName));
        ScriptingEngine.CompileToParseTree(compilation);
        Console.WriteLine(compilation.Parsed!.ToDebugString());
    }

    private static void GenerateCSharp(FileInfo input, FileInfo emitCS)
    {
        var compilation = new Compilation(File.ReadAllText(input.FullName));
        ScriptingEngine.CompileToCSharpCode(compilation);
        Console.WriteLine(compilation.GeneratedCSharp);
        if (emitCS is not null)
        {
            File.WriteAllText(emitCS.FullName, compilation.GeneratedCSharp);
        }
    }

    private static void GenerateExe(FileInfo input, FileInfo output)
    {
        var compilation = new Compilation(File.ReadAllText(input.FullName), output.FullName);
        ScriptingEngine.GenerateExe(compilation);
    }
}
