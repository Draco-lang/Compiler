using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Cli;

internal class Program
{
    internal static int Main(string[] args)
    {
        var fileArgument = new Argument<FileInfo>("--file", description: "Draco file");
        var emitCSOption = new Option<bool>("--emit-cs", () => false, description: "Specifies if generated c# code should be saved to the disk");

        var runCommand = new Command("run", "runs specified draco file")
        {
            fileArgument,
            emitCSOption,
        };
        runCommand.SetHandler((file, emitCS) =>
        {
            Run(file, emitCS);
        }, fileArgument, emitCSOption);

        var generateParseTreeCommand = new Command("parse", "generates parse tree from specified draco file")
        {
            fileArgument,
        };
        generateParseTreeCommand.SetHandler((file) =>
        {
            GenerateParseTree(file);
        }, fileArgument);


        var generateCSCommand = new Command("codegen", "generates c# from specified draco file and displays it to the console")
        {
            fileArgument,
            emitCSOption,
        };
        generateCSCommand.SetHandler((file, emitCS) =>
        {
            GenerateCSharp(file, emitCS);
        }, fileArgument, emitCSOption);

        var generateExeCommand = new Command("compile", "generates executable from specified draco file")
        {
            fileArgument,
            emitCSOption
        };
        generateExeCommand.SetHandler((file, emitCS) =>
        {
            GenerateExe(file, emitCS);
        }, fileArgument, emitCSOption);

        var rootCommand = new RootCommand("CLI for the draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateParseTreeCommand);
        rootCommand.AddCommand(generateCSCommand);
        rootCommand.AddCommand(generateExeCommand);
        return rootCommand.Invoke(args);
    }

    private static void Run(FileInfo fileInfo, bool emitCS) =>
        ScriptingEngine.Execute(File.ReadAllText(fileInfo.FullName), emitCS);

    private static void GenerateParseTree(FileInfo fileInfo)
    {
        var tree = ParseTree.Parse(File.ReadAllText(fileInfo.FullName));
        Console.WriteLine(tree.ToDebugString());
    }

    private static void GenerateCSharp(FileInfo fileInfo, bool emitCS)
    {
        string cSharpCode = ScriptingEngine.GenerateCSharp(File.ReadAllText(fileInfo.FullName));
        Console.WriteLine(cSharpCode);
        if (emitCS)
        {
            File.WriteAllText("transpiledProgram.cs", cSharpCode);
        }
    }

    private static void GenerateExe(FileInfo fileInfo, bool emitCS) =>
        ScriptingEngine.GenerateExe(File.ReadAllText(fileInfo.FullName), emitCS);
}
