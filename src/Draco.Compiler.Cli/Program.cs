using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args) =>
        ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo>("file", description: "Draco file");
        var emitCSharpOutput = new Option<FileInfo>("--output-cs", description: "Specifies output file for generated c#, if not specified, generated code is not saved to the disk");
        var outputOption = new Option<FileInfo>(new string[] { "-o", "--output" }, () => new FileInfo("output"), "Specifies the output file");
        var runCommand = new Command("run", "Runs specified draco file")
        {
            fileArgument,
            outputOption,
        };
        runCommand.SetHandler(Run, fileArgument);

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
        generateCSCommand.SetHandler(GenerateCSharp, fileArgument, emitCSharpOutput);

        var generateExeCommand = new Command("compile", "Generates executable from specified draco file")
        {
            fileArgument,
            outputOption,
        };
        generateExeCommand.SetHandler(GenerateExe, fileArgument, outputOption);

        var rootCommand = new RootCommand("CLI for the draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateParseTreeCommand);
        rootCommand.AddCommand(generateCSCommand);
        rootCommand.AddCommand(generateExeCommand);
        return rootCommand;
    }

    private static void Run(FileInfo input)
    {
        var sourceText = File.ReadAllText(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        var compilation = Compilation.Create(parseTree);
        var execResult = ScriptingEngine.Execute(compilation);
        if (!execResult.Success)
        {
            foreach (var diag in execResult.Diagnostics) Console.WriteLine(diag);
            return;
        }
        Console.WriteLine($"Result: {execResult.Result}");
    }

    private static void GenerateParseTree(FileInfo input)
    {
        var sourceText = File.ReadAllText(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        Console.WriteLine(parseTree.ToDebugString());
    }

    private static void GenerateCSharp(FileInfo input, FileInfo? emitCS)
    {
        var sourceText = File.ReadAllText(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        var compilation = Compilation.Create(parseTree);
        using var csStream = new MemoryStream();
        var emitResult = compilation.EmitCSharp(csStream);
        if (!emitResult.Success)
        {
            foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
            return;
        }
        csStream.Position = 0;
        var generatedCs = new StreamReader(csStream).ReadToEnd();
        Console.WriteLine(generatedCs);
        if (emitCS is not null) File.WriteAllText(emitCS.FullName, generatedCs);
    }

    private static void GenerateExe(FileInfo input, FileInfo output)
    {
        var sourceText = File.ReadAllText(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        var compilation = Compilation.Create(parseTree, output.Name);
        using var dllStream = new FileStream(output.FullName, FileMode.OpenOrCreate);
        var emitResult = compilation.Emit(dllStream);
        if (!emitResult.Success)
        {
            foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
        }
    }
}
