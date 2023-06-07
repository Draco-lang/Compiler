using System;
using System.IO;
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

    protected override string ToolName => Path.GetFileName(this.GetDotNetPath());

    private int errorCount = 0;

    protected override string GenerateCommandLineCommands() => $"exec \"{this.DracoCompilerPath}\"";

    protected override string GenerateResponseFileCommands()
    {
        var sb = new StringBuilder($"compile");
        sb.AppendLine();

        foreach (var file in this.Compile)
        {
            sb.AppendLine(file);
        }

        sb.AppendLine();
        sb.AppendLine($"--output {this.OutputFile} --root-module {this.ProjectDirectory} --pdb --msbuild-diags");

        foreach (var refefence in this.References)
        {
            sb.AppendLine($"-r \"{refefence}\"");
        }
        return sb.ToString();
    }

    protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
    {
        this.errorCount++;
        base.LogEventsFromTextOutput(singleLine, messageImportance);
    }

    protected override bool HandleTaskExecutionErrors()
    {
        if (this.errorCount == 0)
        {
            var message = "Internal compiler error. Please open an issue with a repro case at https://github.com/Draco-lang/Compiler/issues";
            this.Log.LogCriticalMessage(
                subcategory: null, code: "DR0001", helpKeyword: null,
                file: null,
                lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: message.ToString());
        }

        return false;
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

    protected override string GenerateFullPathToTool() => Path.GetFullPath(this.GetDotNetPath());
}
