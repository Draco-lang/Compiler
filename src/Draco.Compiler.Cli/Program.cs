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
        var fileOption = new Argument<FileInfo>("--file", description: "Draco file");
        var emitCSOption = new Option<bool>("--emit-cs", () => false, description: "Specifies if generated c# code should be saved to the disk");
        var emitExeOption = new Option<bool>("--emit-exe", () => false, description: "Specifies if generated executable should be saved to the disk");

        var runCommand = new Command("run", "runs specified draco file")
        {
            fileOption,
            emitCSOption,
            emitExeOption,
        };
        runCommand.SetHandler((file, emitCS, emitExe) =>
        {
            Run(file, emitCS, emitExe);
        }, fileOption, emitCSOption, emitExeOption);

        var generateParseTreeCommand = new Command("parse", "generates parse tree from specified draco file")
        {
            fileOption,
        };
        generateParseTreeCommand.SetHandler((file) =>
        {
            GenerateParseTree(file);
        }, fileOption);


        var generateCSCommand = new Command("codegen", "generates c# from specified draco file and displays it to the console")
        {
            fileOption,
            emitCSOption,
        };
        generateCSCommand.SetHandler((file, emitCS) =>
        {
            GenerateCSharp(file, emitCS);
        }, fileOption, emitCSOption);

        var generateExeCommand = new Command("compile", "generates executable from specified draco file")
        {
            fileOption,
            emitCSOption
        };
        generateExeCommand.SetHandler((file, emitCS) =>
        {
            GenerateExe(file, emitCS);
        }, fileOption, emitCSOption);

        var rootCommand = new RootCommand("CLI for the draco compiler");
        rootCommand.AddCommand(runCommand);
        rootCommand.AddCommand(generateParseTreeCommand);
        rootCommand.AddCommand(generateCSCommand);
        rootCommand.AddCommand(generateExeCommand);
        return rootCommand.Invoke(args);
    }

    private static void Run(FileInfo fileInfo, bool emitCS, bool emitExe)
    {
        ScriptingEngine.Execute(File.ReadAllText(fileInfo.FullName));
    }

    private static void GenerateParseTree(FileInfo fileInfo)
    {
        var tree = ParseTree.Parse(File.ReadAllText(fileInfo.FullName));
        Console.WriteLine(tree.ToDebugString());
    }

    private static void GenerateCSharp(FileInfo fileInfo, bool emitCS)
    {
        Console.WriteLine("CSharp");
    }

    private static void GenerateExe(FileInfo fileInfo, bool emitCS)
    {
        Console.WriteLine("Exe");
    }
}
