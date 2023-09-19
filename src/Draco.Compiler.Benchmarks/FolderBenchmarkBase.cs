using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Draco.Compiler.Benchmarks;

public abstract class FolderBenchmarkBase
{
    [ParamsSource(nameof(GetSourcesFromFolder))]
    public SourceCodeParameter Input { get; set; } = null!;

    private readonly string path;

    protected FolderBenchmarkBase(string path)
    {
        this.path = Path.Join("benchmarks", path);
    }

    public IEnumerable<SourceCodeParameter> GetSourcesFromFolder() => Directory
        .GetFiles(this.path)
        .Select(SourceCodeParameter.FromPath);
}
