using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Utilities;

namespace Draco.Compiler.Toolset;

public sealed class DracoCompiler : ToolTask
{
    public override bool Execute()
    {
        var mainFile = this.Compile.FirstOrDefault(f => f == "main.draco");
        if (mainFile is null)
        {
            this.Log.LogError("File main.draco was not found");
            return false;
        }

        this.ExecuteTool(this.GenerateFullPathToTool(), "", $"exec \"{this.DracoCompilerPath}\" compile {mainFile} --output {this.OutputFile} --msbuild-diags");
        return !this.HasLoggedErrors;
    }

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
