using System;
using System.IO;
using System.Linq;

namespace Draco.Coverage.Cli;

internal static class Program
{
    private static int Main(string[] args)
    {
        args = ProcessArgs(args);

        if (args.Length != 2)
        {
            Log("Usage: Draco.Coverage.Cli <inputPaths> <outputPaths>");
            Log($"Provided argument(s): {string.Join(", ", args)}");
            return 1;
        }

        var inputPaths = args[0];
        var outputPaths = args[1];

        if (string.IsNullOrEmpty(inputPaths) && string.IsNullOrEmpty(outputPaths))
        {
            // Ok, no inputs our outputs
            return 0;
        }

        var inputPathArray = ProcessPath(inputPaths);
        var outputPathArray = ProcessPath(outputPaths);

        if (inputPathArray.Length != outputPathArray.Length)
        {
            Log("Input and output paths must have the same number of elements");
            return 1;
        }

        foreach (var (inputPath, outputPath) in inputPathArray.Zip(outputPathArray))
        {
            if (!Path.Exists(inputPath))
            {
                Log($"Input path '{inputPath}' does not exist");
                return 1;
            }
            if (!Path.Exists(outputPath))
            {
                Log($"Output path '{outputPath}' does not exist");
                return 1;
            }

            InstrumentedAssembly.Weave(inputPath, outputPath);
        }

        return 0;
    }

    private static void Log(string message) => Console.Error.WriteLine(message);

    // MSBuild passes in the path to all binaries copied, concatenated with a semocilon
    // We need to split them and filter for .dll files
    private static string[] ProcessPath(string path) => path
        .Split(';')
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Where(p => Path.GetExtension(p) == ".dll")
        .Select(Path.GetFullPath)
        .ToArray();

    private static string[] ProcessArgs(string[] args)
    {
        if (args.Length != 1) return args;
        if (!args[0].StartsWith('@')) return args;

        // RSP file
        var file = args[0][1..];
        return File.ReadAllLines(file);
    }
}
