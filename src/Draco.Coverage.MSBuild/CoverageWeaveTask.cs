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
        if (this.InputPath != this.OutputPath)
        {
            using var readerStream = File.OpenRead(this.InputPath);
            using var writerStream = File.OpenWrite(this.OutputPath);
            InstrumentedAssembly.Weave(readerStream, writerStream);
        }
        else
        {
            // We use memory streams to avoid locking the file
            using var readerStream = new MemoryStream(File.ReadAllBytes(this.InputPath));
            using var writerStream = new MemoryStream();
            InstrumentedAssembly.Weave(readerStream, writerStream);
            File.WriteAllBytes(this.OutputPath, writerStream.ToArray());
        }

        return true;
    }
}
