using System;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args) =>
        ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo[]>("files", description: "The Draco source files to compile");
        var emitIROutputOption = new Option<FileInfo>("--output-ir", description: "Specifies output file for generated IR, if not specified, generated code is not saved to the disk");
        var outputOption = new Option<FileInfo>(new string[] { "-o", "--output" }, () => new FileInfo("output"), "Specifies the output file");
        var msbuildDiagOption = new Option<bool>("--msbuild-diags", () => false, description: "Specifies if diagnostics should be returned in MSBuild diagnostic format");

        var runCommand = new Command("run", "Runs specified Draco file")
        {
            fileArgument,
            outputOption,
        };
        runCommand.SetHandler(Run, fileArgument);

        var generateIRCommand = new Command("codegen", "Generates DracoIR from specified draco file and displays it to the console")
        {
            fileArgument,
            emitIROutputOption,
        };
        generateIRCommand.SetHandler(GenerateDracoIR, fileArgument, emitIROutputOption);

        var generateExeCommand = new Command("compile", "Generates executable from specified Draco file")
        {
            fileArgument,
            outputOption,
            msbuildDiagOption,
        };
        generateExeCommand.SetHandler(GenerateExe, fileArgument, outputOption, msbuildDiagOption);

        var formatCodeCommand = new Command("format", "Formats contents of specified Draco file and writes formatted code to the standard output")
        {
            fileArgument,
        };
        formatCodeCommand.SetHandler(FormatCode, fileArgument);

        var rootCommand = new RootCommand("CLI for the Draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateIRCommand);
        rootCommand.AddCommand(generateExeCommand);
        rootCommand.AddCommand(formatCodeCommand);
        return rootCommand;
    }

    private static void Run(FileInfo[] input)
    {
        var sourceTexts = input.Select(i => SourceText.FromFile(i.FullName));
        var syntaxTrees = sourceTexts.Select(SyntaxTree.Parse);
        var compilation = Compilation.Create(syntaxTrees.ToImmutableArray());
        compilation.Dump();
    }

    private static void GenerateDracoIR(FileInfo[] input, FileInfo? emitCS)
    {
        var sourceTexts = input.Select(i => SourceText.FromFile(i.FullName));
        var syntaxTrees = sourceTexts.Select(SyntaxTree.Parse);
        var compilation = Compilation.Create(syntaxTrees.ToImmutableArray());
        compilation.Dump();
    }

    private static void GenerateExe(FileInfo[] input, FileInfo output, bool msbuildDiags) =>
        throw new NotImplementedException();

    private static string MakeMsbuildDiag(Diagnostic original)
    {
        var file = string.Empty;
        if (!original.Location.IsNone && original.Location.SourceText.Path is not null)
        {
            var range = original.Location.Range!.Value;
            file = $"{original.Location.SourceText.Path.OriginalString}({range.Start.Line + 1},{range.Start.Column + 1},{range.End.Line + 1},{range.End.Column + 1})";
        }
        return $"{file} : {original.Severity.ToString().ToLower()} {original.Template.Code} : {original.Message}";
    }

    private static void FormatCode(FileInfo[] input) => throw new NotImplementedException();
}
