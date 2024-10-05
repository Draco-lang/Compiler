using System.Collections.Generic;
using System.IO;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace Draco.Compiler.Benchmarks;

public abstract class FolderBenchmarkBase(string path)
{
    [ParamsSource(nameof(GetSourcesFromFolder))]
    public SourceCodeParameter Input { get; set; } = null!;

    public IEnumerable<SourceCodeParameter> GetSourcesFromFolder() => Directory
        .GetFiles(path)
        .Select(SourceCodeParameter.FromPath);
}
