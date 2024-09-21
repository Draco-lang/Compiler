using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Draco.Coverage.Tasks;

public sealed class CoverageWeaveTask : ToolTask
{
    public string WeaverPath { get; set; }
    public string InputPaths { get; set; }
    public string OutputPaths { get; set; }

    protected override string ToolName => Path.GetFileName(GetDotNetPath());

    private int errorCount = 0;

    protected override string GenerateCommandLineCommands() => $"exec \"{this.WeaverPath}\"";

    protected override string GenerateResponseFileCommands()
    {
        var sb = new StringBuilder();
        sb.AppendLine(this.InputPaths);
        sb.AppendLine(this.OutputPaths);
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
            var message = "Internal weaver error. Please open an issue with a repro case at https://github.com/Draco-lang/Compiler/issues";
            this.Log.LogCriticalMessage(
                subcategory: null, code: "DRC0001", helpKeyword: null,
                file: null,
                lineNumber: 0, columnNumber: 0,
                endLineNumber: 0, endColumnNumber: 0,
                message: message.ToString());
        }

        return false;
    }

    private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

    // https://github.com/dotnet/roslyn/blob/020db28fa9b744146e6f072dbdc6bf3e62c901c1/src/Compilers/Shared/RuntimeHostInfo.cs#L59
    private static string GetDotNetPath()
    {
        if (Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName) is string pathToDotNet)
        {
            return pathToDotNet;
        }

        var (fileName, sep) = Environment.OSVersion.Platform == PlatformID.Win32NT
            ? ("dotnet.exe", ';')
            : ("dotnet", ':');

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var item in path.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var filePath = Path.Combine(item, fileName);
                if (File.Exists(filePath))
                {
                    return filePath;
                }
            }
            catch
            {
                // If we can't read a directory for any reason just skip it
            }
        }

        return fileName;
    }

    protected override string GenerateFullPathToTool() => Path.GetFullPath(GetDotNetPath());
}
