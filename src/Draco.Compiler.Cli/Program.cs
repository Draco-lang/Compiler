using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Extensions;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args) =>
        ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo>("source file", description: "The Draco source file");
        var outputOption = new Option<FileInfo>(["-o", "--output"], () => new FileInfo("output"), "Specifies the output file");
        var optionalOutputOption = new Option<FileInfo?>(["-o", "--output"], () => null, "Specifies the (optional) output file");
        var referencesOption = new Option<FileInfo[]>(["-r", "--reference"], Array.Empty<FileInfo>, "Specifies assembly references to use when compiling");
        var filesArgument = new Argument<FileInfo[]>("source files", Array.Empty<FileInfo>, "Specifies draco source files that should be compiled");
        var rootModuleOption = new Option<DirectoryInfo?>(["-m", "--root-module"], () => null, "Specifies the root module folder of the compiled files");
        var pdbOption = new Option<bool>("--pdb", () => false, "Specifies that a PDB should be generated for debugging");
        var msbuildDiagOption = new Option<bool>("--msbuild-diags", () => false, description: "Specifies if diagnostics should be returned in MSBuild diagnostic format");

        // Compile

        var compileCommand = new Command("compile", "Compiles the Draco program")
        {
            filesArgument,
            outputOption,
            rootModuleOption,
            referencesOption,
            pdbOption,
            msbuildDiagOption,
        };
        compileCommand.SetHandler(CompileCommand, filesArgument, outputOption, rootModuleOption, referencesOption, pdbOption, msbuildDiagOption);

        // Run

        var runCommand = new Command("run", "Runs the Draco program")
        {
            filesArgument,
            rootModuleOption,
            referencesOption,
            msbuildDiagOption,
        };
        runCommand.SetHandler(RunCommand, filesArgument, rootModuleOption, referencesOption, msbuildDiagOption);

        // IR code

        var irCommand = new Command("ir", "Generates the intermediate-representation of the Draco program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
            msbuildDiagOption,
        };
        irCommand.SetHandler(IrCommand, filesArgument, rootModuleOption, optionalOutputOption, msbuildDiagOption);

        // Symbol tree

        var symbolsCommand = new Command("symbols", "Prints the symbol-tree of the program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
            msbuildDiagOption,
        };
        symbolsCommand.SetHandler(SymbolsCommand, filesArgument, rootModuleOption, optionalOutputOption, msbuildDiagOption);

        // Declaration tree

        var declarationsCommand = new Command("declarations", "Prints the declarations-tree of the program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
            msbuildDiagOption,
        };
        declarationsCommand.SetHandler(DeclarationsCommand, filesArgument, rootModuleOption, optionalOutputOption, msbuildDiagOption);

        // Formatting

        var formatCommand = new Command("format", "Formats contents of the specified Draco file")
        {
            fileArgument,
            optionalOutputOption,
        };
        formatCommand.SetHandler(FormatCommand, fileArgument, optionalOutputOption);

        return new RootCommand("CLI for the Draco compiler")
        {
            compileCommand,
            runCommand,
            irCommand,
            symbolsCommand,
            declarationsCommand,
            formatCommand
        };
    }

    private static void CompileCommand(FileInfo[] input, FileInfo output, DirectoryInfo? rootModule, FileInfo[] references, bool emitPdb, bool msbuildDiags)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var (path, name) = ExtractOutputPathAndName(output);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: references
                .Select(r => MetadataReference.FromFile(r.FullName))
                .ToImmutableArray(),
            rootModulePath: rootModule?.FullName,
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

    private static void RunCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo[] references, bool msbuildDiags)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: references
                .Select(r => MetadataReference.FromFile(r.FullName))
                .ToImmutableArray(),
            rootModulePath: rootModule?.FullName);
        var execResult = Script.ExecuteAsProgram(compilation);
        if (!EmitDiagnostics(execResult, msbuildDiags))
        {
            Console.WriteLine($"Result: {execResult.Value}");
        }
    }

    private static void IrCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output, bool msbuildDiags)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            rootModulePath: rootModule?.FullName);
        using var irStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(irStream: irStream);
        EmitDiagnostics(emitResult, msbuildDiags);
    }

    private static void SymbolsCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output, bool msbuildDiags)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            rootModulePath: rootModule?.FullName);
        using var symbolsStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(symbolTreeStream: symbolsStream);
        EmitDiagnostics(emitResult, msbuildDiags);
    }

    private static void DeclarationsCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output, bool msbuildDiags)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            rootModulePath: rootModule?.FullName);
        using var declarationStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(declarationTreeStream: declarationStream);
        EmitDiagnostics(emitResult, msbuildDiags);
    }

    private static void FormatCommand(FileInfo input, FileInfo? output)
    {
        var syntaxTree = GetSyntaxTrees(input).First();
        using var outputStream = OpenOutputOrStdout(output);
        new StreamWriter(outputStream).Write(syntaxTree.Format().ToString());
    }

    private static ImmutableArray<SyntaxTree> GetSyntaxTrees(params FileInfo[] input)
    {
        var result = ImmutableArray.CreateBuilder<SyntaxTree>();
        foreach (var file in input)
        {
            var sourceText = SourceText.FromFile(file.FullName);
            result.Add(SyntaxTree.Parse(sourceText));
        }
        return result.ToImmutable();
    }

    private static bool EmitDiagnostics(EmitResult result, bool msbuildDiags)
    {
        if (result.Success) return false;
        WriteDiagnostics(result.Diagnostics, msbuildDiags);
        return true;
    }

    private static bool EmitDiagnostics<T>(ExecutionResult<T> result, bool msbuildDiags)
    {
        if (result.Success) return false;
        WriteDiagnostics(result.Diagnostics, msbuildDiags);
        return true;
    }

    private static void WriteDiagnostics(IEnumerable<Diagnostic> diagnostics, bool msbuildDiags)
    {
        foreach (var diag in diagnostics)
        {
            Console.Error.WriteLine(msbuildDiags ? diag.ToMsbuildString() : diag.ToString());
        }
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
