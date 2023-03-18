using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

namespace Draco.Compiler.Tasks;

public sealed class DracoCompiler : ToolTask
{
    /// <summary>
    /// Output type of the given project.
    /// </summary>
    public string OutputType { get; set; }

    /// <summary>
    /// The directory the current project is located in.
    /// </summary>
    public string ProjectDirectory { get; set; }

    /// <summary>
    /// Name of the current project.
    /// </summary>
    public string ProjectName { get; set; }

    /// <summary>
    /// Output file, it will be located in obj folder and copied by msbuild to bin.
    /// </summary>
    public string OutputFile { get; set; }

    /// <summary>
    /// Array of files that should be compiled.
    /// </summary>
    public string[] Compile { get; set; }

    /// <summary>
    /// Path to the DLL implementing the draco compiler commandline.
    /// </summary>
    public string DracoCompilerPath { get; set; }

    public override bool Execute()
    {
        var mainFile = this.Compile.FirstOrDefault(f => f == "main.draco");
        if (mainFile is null)
        {
            this.Log.LogError("File main.draco was not found");
            return false;
        }

        var exitCode = this.ExecuteTool(this.GenerateFullPathToTool(), "", $"exec \"{this.DracoCompilerPath}\" compile {mainFile} --output {this.OutputFile} --msbuild-diags");
        // Checking for compiler crash
        if (!this.HasLoggedErrors && exitCode != 0)
        {
            this.LogEventsFromTextOutput("draco compiler : error DR0001 : The compiler failed unexpectedly, please report this as bug.", Microsoft.Build.Framework.MessageImportance.Normal); // TODO: Is this the correct way?
        }
        return !this.HasLoggedErrors;
    }

    // If the targets do not set the ToolPath and ToolExe, we fall back to the below logic.

    private string GetDotNetPath()
    {
        const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        var path = Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName);
        if (string.IsNullOrEmpty(path))
        {
            throw new InvalidOperationException($"{DotNetHostPathEnvironmentName} is not set");
        }

        return path;
    }

    protected override string ToolName => Path.GetFileName(this.GetDotNetPath());

    protected override string GenerateFullPathToTool() => Path.GetFullPath(this.GetDotNetPath());
}
