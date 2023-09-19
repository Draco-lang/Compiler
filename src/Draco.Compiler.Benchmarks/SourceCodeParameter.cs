using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Draco.Compiler.Benchmarks;

public sealed record class SourceCodeParameter(string Path, string Code)
{
    public static SourceCodeParameter FromPath(string path) => new(path, File.ReadAllText(path));

    public override string ToString() => System.IO.Path.GetFileName(this.Path);
}
