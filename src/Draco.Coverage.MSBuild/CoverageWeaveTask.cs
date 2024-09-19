using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Draco.Coverage.MSBuild;

public sealed class CoverageWeaveTask : Task
{
    public string InputPath { get; set; }

    public string OutputPath { get; set; }

    public override bool Execute()
    {
        var inputPaths = ProcessPath(this.InputPath);
        var outputPaths = ProcessPath(this.OutputPath);
        if (inputPaths.Length != outputPaths.Length)
        {
            throw new ArgumentException("input and output paths must have the same number of elements");
        }

        for (var i = 0; i < inputPaths.Length; i++)
        {
            this.Weave(inputPaths[i], outputPaths[i]);
        }

        return true;
    }

    private void Weave(string inputPath, string outputPath) => InstrumentedAssembly.Weave(inputPath, outputPath);

    // MSBuild passes in the path to all binaries copied, concatenated with a semocilon
    // We need to split them and filter for .dll files
    private static string[] ProcessPath(string path) => path
        .Split(';')
        .Where(p => !string.IsNullOrWhiteSpace(p))
        .Where(p => Path.GetExtension(p) == ".dll")
        .ToArray();
}
