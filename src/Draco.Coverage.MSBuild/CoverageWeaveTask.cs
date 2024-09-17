using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Draco.Coverage.MSBuild;

public sealed class CoverageWeaveTask : Task
{
    [Required]
    public string InputPath { get; set; }

    [Required]
    public string OutputPath { get; set; }

    public override bool Execute()
    {
        File.AppendAllText("C:/TMP/tasklog.txt", $"InputPath: {this.InputPath}\n");
        File.AppendAllText("C:/TMP/tasklog.txt", $"OutputPath: {this.OutputPath}\n");

        using var readerStream = File.OpenRead(this.InputPath);
        using var writerStream = File.OpenWrite(this.OutputPath);
        InstrumentedAssembly.Weave(readerStream, writerStream);

        readerStream.Close();
        writerStream.Close();

        return true;
    }
}
