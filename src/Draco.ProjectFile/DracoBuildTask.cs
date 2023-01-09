using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Utilities;

namespace Draco.ProjectFile;

public sealed class DracoBuildTask : ToolTask
{
    public override bool Execute()
    {
        var files = Directory.EnumerateFiles(this.ProjectDirectory, "*.draco", SearchOption.AllDirectories);
        var mainFile = files.FirstOrDefault(f => f == Path.Combine(this.ProjectDirectory, "main.draco"));
        if (mainFile is null)
        {
            this.Log.LogError("File main.draco was not found");
            return false;
        }
        this.ExecuteTool(this.GenerateFullPathToTool(), "", $"compile {mainFile} --output {this.OutputFile} --msbuild-diags");
        if (this.HasLoggedErrors) return false;
        return true;
    }

    protected override string GenerateFullPathToTool() => Path.GetFullPath(Path.Combine(Assembly.GetExecutingAssembly().Location, @"..\..\..\..\..\Draco.Compiler.Cli\bin\Debug\net7.0\Draco.Compiler.Cli.exe"));

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
