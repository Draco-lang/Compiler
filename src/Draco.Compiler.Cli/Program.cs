using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Diagnostics;
using System.Linq;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args) =>
        ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo>("file", description: "Draco file");
        var emitIROutputOption = new Option<FileInfo>("--output-ir", description: "Specifies output file for generated IR, if not specified, generated code is not saved to the disk");
        var outputOption = new Option<FileInfo>(new string[] { "-o", "--output" }, () => new FileInfo("output"), description: "Specifies the output file");
        var msbuildDiagOption = new Option<bool>("--msbuild-diags", () => false, description: "Specifies if diagnostics should be returned in msbuild diagnostic format");
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

        var generateIRCommand = new Command("codegen", "Generates DracoIR from specified draco file and displays it to the console")
        {
            fileArgument,
            emitIROutputOption,
        };
        generateIRCommand.SetHandler(GenerateDracoIR, fileArgument, emitIROutputOption);

        var generateExeCommand = new Command("compile", "Generates executable from specified draco file")
        {
            fileArgument,
            outputOption,
            msbuildDiagOption,
        };
        generateExeCommand.SetHandler(GenerateExe, fileArgument, outputOption, msbuildDiagOption);

        var rootCommand = new RootCommand("CLI for the draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateParseTreeCommand);
        rootCommand.AddCommand(generateIRCommand);
        rootCommand.AddCommand(generateExeCommand);
        return rootCommand;
    }

    private static void Run(FileInfo input)
    {
        var sourceText = SourceText.FromFile(input.FullName);
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
        var sourceText = SourceText.FromFile(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        Console.WriteLine(parseTree.Root.ToDebugString());
    }

    private static void GenerateDracoIR(FileInfo input, FileInfo? emitCS)
    {
        var sourceText = SourceText.FromFile(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        var compilation = Compilation.Create(parseTree);
        using var irStream = new MemoryStream();
        var emitResult = compilation.Emit(
            peStream: new MemoryStream(),
            dracoIrStream: irStream);
        if (!emitResult.Success)
        {
            foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
            return;
        }
        irStream.Position = 0;
        var generatedIr = new StreamReader(irStream).ReadToEnd();
        Console.WriteLine(generatedIr);
        if (emitCS is not null) File.WriteAllText(emitCS.FullName, generatedIr);
    }

    private static void GenerateExe(FileInfo input, FileInfo output, bool jsonOutput)
    {
        var sourceText = SourceText.FromFile(input.FullName);
        var parseTree = ParseTree.Parse(sourceText);
        var compilation = Compilation.Create(parseTree, output.Name);
        using var dllStream = new FileStream(output.FullName, FileMode.OpenOrCreate);
        var emitResult = compilation.Emit(dllStream);
        if (!emitResult.Success)
        {
            if (jsonOutput) foreach (var diag in emitResult.Diagnostics) if (diag.Location.IsNone) Console.WriteLine($"{diag.Severity.ToString().ToLower()} 000 : {diag.Message}");
                    else Console.WriteLine($"""
                        {diag.Location.SourceText.Path!.OriginalString}({diag.Location.Range!.Value.Start.Line + 1}, {diag.Location.Range!.Value.Start.Column + 1},
                        {diag.Location.Range!.Value.End.Line + 1}, {diag.Location.Range!.Value.End.Column + 1}) : {diag.Severity.ToString().ToLower()} {diag.ErrorCode} : {diag.Message}
                        """.ReplaceLineEndings(""));
            else foreach (var diag in emitResult.Diagnostics) Console.WriteLine(diag);
        }
    }
}
