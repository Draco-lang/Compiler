using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Microsoft.Build.Framework;
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
    /// Array of DLLs to use as references.
    /// </summary>
    public string[] References { get; set; }

    /// <summary>
    /// Path to the DLL implementing the draco compiler commandline.
    /// </summary>
    public string DracoCompilerPath { get; set; }

    protected override bool ValidateParameters()
    {
        var mainFile = this.Compile.FirstOrDefault(f => f == "main.draco");
        if (mainFile is null)
        {
            this.Log.LogError("File main.draco was not found");
            return false;
        }

        return true;
    }

    protected override string GenerateCommandLineCommands()
    {
        return $"exec \"{this.DracoCompilerPath}\"";
    }

    protected override string GenerateResponseFileCommands()
    {
        var mainFile = this.Compile.First(f => f == "main.draco");

        var sb = new StringBuilder($"compile {mainFile} --output {this.OutputFile} --msbuild-diags");
        sb.AppendLine();

        foreach (var file in this.References)
        {
            sb.AppendLine($"-r \"{file}\"");
        }

        return sb.ToString();
    }

    private List<string> errorLines;

    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
        if (messageImportance == MessageImportance.Normal) // was singleLine read from standard error?
        {
            this.errorLines ??= new();
            this.errorLines.Add(singleLine);
        }
    }

    protected override bool HandleTaskExecutionErrors()
    {
        if (this.errorLines?.Count > 0)
        {
            this.Log.LogCriticalMessage(null, "DR0001", null, null, 0, 0, 0, 0,
                "Internal compiler error. Please open an issue with a repro case at https://github.com/Draco-lang/Compiler/issues"
                + Environment.NewLine
                + string.Join(Environment.NewLine, this.errorLines));
        }

        return base.HandleTaskExecutionErrors();
    }

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
