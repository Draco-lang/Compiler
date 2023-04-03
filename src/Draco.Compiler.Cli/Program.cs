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
        var fileArgument = new Argument<FileInfo>("file", description: "The Draco source file");
        var outputOption = new Option<FileInfo>(new string[] { "-o", "--output" }, () => new FileInfo("output"), "Specifies the output file");
        var optionalOutputOption = new Option<FileInfo?>(new string[] { "-o", "--output" }, () => null, "Specifies the (optional) output file");
        var pdbOption = new Option<bool>("--pdb", () => false, "Specifies that a PDB should be generated for debugging");
        var msbuildDiagOption = new Option<bool>("--msbuild-diags", () => false, description: "Specifies if diagnostics should be returned in MSBuild diagnostic format");

        // Compile

        var compileCommand = new Command("compile", "Compiles the Draco program")
        {
            fileArgument,
            outputOption,
            pdbOption,
            msbuildDiagOption,
        };
        compileCommand.SetHandler(CompileCommand, fileArgument, outputOption, pdbOption, msbuildDiagOption);

        // Run

        var runCommand = new Command("run", "Runs the Draco program")
        {
            fileArgument,
            msbuildDiagOption,
        };
        runCommand.SetHandler(RunCommand, fileArgument, msbuildDiagOption);

        // IR code

        var irCommand = new Command("ir", "Generates the intermediate-representation of the Draco program")
        {
            fileArgument,
            optionalOutputOption,
            msbuildDiagOption,
        };
        irCommand.SetHandler(IrCommand, fileArgument, optionalOutputOption, msbuildDiagOption);

        // Formatting

        var formatCommand = new Command("format", "Formats contents of the specified Draco file")
        {
            fileArgument,
            optionalOutputOption,
        };
        formatCommand.SetHandler(FormatCommand, fileArgument, optionalOutputOption);

        var rootCommand = new RootCommand("CLI for the Draco compiler");
        rootCommand.AddCommand(compileCommand);
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(irCommand);
        rootCommand.AddCommand(formatCommand);
        return rootCommand;
    }

    private static void CompileCommand(FileInfo input, FileInfo output, bool emitPdb, bool msbuildDiags)
    {
        var syntaxTree = GetSyntaxTree(input);
        var (path, name) = ExtractOutputPathAndName(output);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree),
            outputPath: path,
            assemblyName: name);
        using var peStream = new FileStream(Path.ChangeExtension(output.FullName, ".dll"), FileMode.OpenOrCreate);
        using var pdbStream = emitPdb
            ? new FileStream(Path.ChangeExtension(output.FullName, ".pdb"), FileMode.OpenOrCreate)
            : null;
        var emitResult = compilation.Emit(
            peStream: peStream,
            pdbStream: pdbStream);
        EmitDiagnostics(emitResult, msbuildDiags);
    }

    private static void RunCommand(FileInfo input, bool msbuildDiags)
    {
        var syntaxTree = GetSyntaxTree(input);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        var execResult = ScriptingEngine.Execute(compilation);
        if (!EmitDiagnostics(execResult, msbuildDiags))
        {
            Console.WriteLine($"Result: {execResult.Result}");
        }
    }

    private static void IrCommand(FileInfo input, FileInfo? output, bool msbuildDiags)
    {
        var syntaxTree = GetSyntaxTree(input);
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(syntaxTree));
        using var irStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(irStream: irStream);
        EmitDiagnostics(emitResult, msbuildDiags);
    }

    private static void FormatCommand(FileInfo input, FileInfo? output)
    {
        var syntaxTree = GetSyntaxTree(input);
        using var outputStream = OpenOutputOrStdout(output);
        new StreamWriter(outputStream).Write(syntaxTree.Format().ToString());
    }

    private static SyntaxTree GetSyntaxTree(FileInfo input)
    {
        var sourceText = SourceText.FromFile(input.FullName);
        return SyntaxTree.Parse(sourceText);
    }

    private static bool EmitDiagnostics(EmitResult result, bool msbuildDiags)
    {
        if (result.Success) return false;
        foreach (var diag in result.Diagnostics)
        {
            Console.Error.WriteLine(msbuildDiags ? MakeMsbuildDiag(diag) : diag.ToString());
        }
        return true;
    }

    private static bool EmitDiagnostics<T>(ExecutionResult<T> result, bool msbuildDiags)
    {
        if (result.Success) return false;
        foreach (var diag in result.Diagnostics)
        {
            Console.Error.WriteLine(msbuildDiags ? MakeMsbuildDiag(diag) : diag.ToString());
        }
        return true;
    }

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

    private static (string Path, string Name) ExtractOutputPathAndName(FileInfo outputInfo)
    {
        var outputPath = outputInfo.FullName;
        var path = Path.GetDirectoryName(outputPath) ?? string.Empty;
        path = Path.GetFullPath(path);
        var name = Path.GetFileNameWithoutExtension(outputPath) ?? string.Empty;
        return (path, name);
    }

    private static Stream OpenOutputOrStdout(FileInfo? output) => output is null
        ? Console.OpenStandardOutput()
        : output.Open(FileMode.OpenOrCreate, FileAccess.Write);
}
