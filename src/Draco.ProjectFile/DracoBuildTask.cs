using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Framework;

namespace Draco.ProjectFile;

public class DracoBuildTask : Microsoft.Build.Utilities.ToolTask
{
    public override bool Execute()
    {
        var files = Directory.EnumerateFiles(this.ProjectDirectory, "*.draco", SearchOption.TopDirectoryOnly).Where(x => x == Path.Combine(this.ProjectDirectory, "main.draco"));
        if (files.Count() == 0) return false;
        var mainFile = files.First();
        var output = $"{this.ProjectName}.exe";

        this.ExecuteTool(this.GenerateFullPathToTool(), "", $"compile {mainFile} {this.OutputFile.ToCliFlag("output")}");
        // TODO: Only emit if there were no errors while compiling
        //File.WriteAllText($"{this.ProjectName}.runtimeconfig.json", this.GenerateRuntimeConfigContents());
        // TODO: Retarget standard output and show diags as errors/warnings/messages in the correct colors
        return true;
    }

    protected override string GenerateFullPathToTool() => Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\Draco.Compiler.Cli\bin\Debug\net7.0\Draco.Compiler.Cli.exe"));

    // TODO: change the target frameword based on users choise
    private string GenerateRuntimeConfigContents() => """
            {
              "runtimeOptions": {
                "tfm": "net7.0",
                "framework": {
                  "name": "Microsoft.NETCore.App",
                  "version": "7.0.0"
                }
              }
            }
            """;
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

    protected override string ToolName => "Draco.Compiler.Cli.exe";
}
internal static class CliFlag
{
    public static string ToCliFlag(this object flagValue, string flagName) => $"--{flagName} {flagValue}";
}
