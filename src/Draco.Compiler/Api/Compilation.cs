using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents a single compilation session.
/// </summary>
public sealed class Compilation
{
    public string Source { get; }

    public ParseTree? Parsed { get; internal set; }

    public string? GeneratedCSharp { get; internal set; }

    public FileInfo? CompiledExecutablePath { get; }

    public Compilation(string source, string? outputFile = null)
    {
        this.Source = source;
        if (outputFile is not null) this.CompiledExecutablePath = new FileInfo(outputFile);
    }
}
