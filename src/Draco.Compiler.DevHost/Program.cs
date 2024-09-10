using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.IO;
using System.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api.Syntax.Quoting;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.DevHost;

internal class Program
{
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    internal static int Main(string[] args) =>
        ConfigureCommands().Invoke(args);

    private static RootCommand ConfigureCommands()
    {
        var fileArgument = new Argument<FileInfo>("source file", description: "The Draco source file");
        var outputOption = new Option<FileInfo>(["-o", "--output"], () => new FileInfo("output"), "Specifies the output file");
        var optionalOutputOption = new Option<FileInfo?>(["-o", "--output"], () => null, "Specifies the (optional) output file");
        var referencesOption = new Option<FileInfo[]>(["-r", "--reference"], Array.Empty<FileInfo>, "Specifies additional assembly references to use when compiling");
        var filesArgument = new Argument<FileInfo[]>("source files", Array.Empty<FileInfo>, "Specifies draco source files that should be compiled");
        var rootModuleOption = new Option<DirectoryInfo?>(["-m", "--root-module"], () => null, "Specifies the root module folder of the compiled files");
        var quoteModeOption = new Option<QuoteMode>(["-m", "--quote-mode"], () => QuoteMode.File, "Specifies the kind of syntactic element to quote");
        var outputLanguageOption = new Option<OutputLanguage>(["-l", "--language"], () => OutputLanguage.CSharp, "Specifies the language to output the quoted code as");
        var prettyPrintOption = new Option<bool>(["-p", "--pretty-print"], () => false, "Whether to append whitespace to the output code");
        var staticImportOption = new Option<bool>(["-s", "--require-static-import"], () => false, "Whether to require the SyntaxFactory class to be imported statically in the output code");

        // Compile

        var compileCommand = new Command("compile", "Compiles the Draco program")
        {
            filesArgument,
            outputOption,
            rootModuleOption,
            referencesOption,
        };
        compileCommand.SetHandler(CompileCommand, filesArgument, outputOption, rootModuleOption, referencesOption);

        // Run

        var runCommand = new Command("run", "Runs the Draco program")
        {
            filesArgument,
            rootModuleOption,
            referencesOption,
        };
        runCommand.SetHandler(RunCommand, filesArgument, rootModuleOption, referencesOption);

        // IR code

        var irCommand = new Command("ir", "Generates the intermediate-representation of the Draco program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
        };
        irCommand.SetHandler(IrCommand, filesArgument, rootModuleOption, optionalOutputOption);

        // Symbol tree

        var symbolsCommand = new Command("symbols", "Prints the symbol-tree of the program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
        };
        symbolsCommand.SetHandler(SymbolsCommand, filesArgument, rootModuleOption, optionalOutputOption);

        // Declaration tree

        var declarationsCommand = new Command("declarations", "Prints the declarations-tree of the program")
        {
            filesArgument,
            rootModuleOption,
            optionalOutputOption,
        };
        declarationsCommand.SetHandler(DeclarationsCommand, filesArgument, rootModuleOption, optionalOutputOption);

        // Formatting

        var formatCommand = new Command("format", "Formats contents of the specified Draco file")
        {
            fileArgument,
            optionalOutputOption,
        };
        formatCommand.SetHandler(FormatCommand, fileArgument, optionalOutputOption);

        // Quoting

        var quoteCommand = new Command("quote", "Generates a string of code in an output language which produces syntax nodes equivalent to the input code")
        {
            fileArgument,
            quoteModeOption,
            outputLanguageOption,
            prettyPrintOption,
            staticImportOption,
        };
        quoteCommand.SetHandler(QuoteCommand, fileArgument, quoteModeOption, outputLanguageOption, prettyPrintOption, staticImportOption);

        return new RootCommand("CLI for the Draco compiler")
        {
            compileCommand,
            runCommand,
            irCommand,
            symbolsCommand,
            declarationsCommand,
            formatCommand,
            quoteCommand,
        };
    }

    private static void CompileCommand(FileInfo[] input, FileInfo output, DirectoryInfo? rootModule, FileInfo[] references)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var (path, name) = ExtractOutputPathAndName(output);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: references
                .Select(r => MetadataReference.FromPeStream(r.OpenRead()))
                .Concat(BclReferences)
                .ToImmutableArray(),
            rootModulePath: rootModule?.FullName,
            outputPath: path,
            assemblyName: name);
        using var peStream = new FileStream(Path.ChangeExtension(output.FullName, ".dll"), FileMode.OpenOrCreate);
        using var pdbStream = new FileStream(Path.ChangeExtension(output.FullName, ".pdb"), FileMode.OpenOrCreate);
        var emitResult = compilation.Emit(
            peStream: peStream,
            pdbStream: pdbStream);
        EmitDiagnostics(emitResult);
    }

    private static void RunCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo[] references)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            metadataReferences: references
                .Select(r => MetadataReference.FromPeStream(r.OpenRead()))
                .Concat(BclReferences)
                .ToImmutableArray(),
            rootModulePath: rootModule?.FullName);
        var execResult = Script.ExecuteAsProgram(compilation);
        if (!EmitDiagnostics(execResult))
        {
            Console.WriteLine($"Result: {execResult.Value}");
        }
    }

    private static void IrCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            // TODO: Add references from CLI?
            metadataReferences: BclReferences.ToImmutableArray(),
            rootModulePath: rootModule?.FullName);
        using var irStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(irStream: irStream);
        EmitDiagnostics(emitResult);
    }

    private static void SymbolsCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            rootModulePath: rootModule?.FullName);
        using var symbolsStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(symbolTreeStream: symbolsStream);
        EmitDiagnostics(emitResult);
    }

    private static void DeclarationsCommand(FileInfo[] input, DirectoryInfo? rootModule, FileInfo? output)
    {
        var syntaxTrees = GetSyntaxTrees(input);
        var compilation = Compilation.Create(
            syntaxTrees: syntaxTrees,
            rootModulePath: rootModule?.FullName);
        using var declarationStream = OpenOutputOrStdout(output);
        var emitResult = compilation.Emit(declarationTreeStream: declarationStream);
        EmitDiagnostics(emitResult);
    }

    private static void FormatCommand(FileInfo input, FileInfo? output)
    {
        var syntaxTree = GetSyntaxTrees(input).First();
        using var outputStream = OpenOutputOrStdout(output);
        new StreamWriter(outputStream).Write(syntaxTree.Format().ToString());
    }

    private static void QuoteCommand(FileInfo input, QuoteMode mode, OutputLanguage outputLanguage, bool prettyPrint, bool staticImport)
    {
        var sourceText = SourceText.FromFile(input.FullName);
        var quotedText = SyntaxQuoter.Quote(sourceText, mode, outputLanguage, prettyPrint, staticImport);
        Console.WriteLine(quotedText);
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

    private static bool EmitDiagnostics(EmitResult result)
    {
        if (result.Success) return false;
        foreach (var diag in result.Diagnostics)
        {
            Console.Error.WriteLine(diag.ToString());
        }
        return true;
    }

    private static bool EmitDiagnostics<T>(ExecutionResult<T> result)
    {
        if (result.Success) return false;
        foreach (var diag in result.Diagnostics)
        {
            Console.Error.WriteLine(diag.ToString());
        }
        return true;
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
