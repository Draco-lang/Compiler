using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Codegen;
using Draco.Query;

namespace Draco.Compiler.Api;

/// <summary>
/// Represents a single compilation session.
/// </summary>
public sealed class Compilation
{
    private readonly QueryDatabase db = new();

    public string Source { get; }

    public ParseTree? Parsed { get; internal set; }

    public string? GeneratedCSharp { get; internal set; }

    public FileInfo? CompiledExecutablePath { get; }

    public Compilation(string source, string? outputFile = null)
    {
        this.Source = source;
        if (outputFile is not null) this.CompiledExecutablePath = new FileInfo(outputFile);
    }

    /// <summary>
    /// Retrieves the <see cref="SemanticModel"/> for a tree.
    /// </summary>
    /// <param name="tree">The <see cref="ParseTree"/> root to retrieve the model for.</param>
    /// <returns>The <see cref="SemanticModel"/> with <paramref name="tree"/> as the root.</returns>
    public SemanticModel GetSemanticModel(ParseTree tree) =>
        new(this.db, tree);

    /// <summary>
    /// Emits compiled C# code to a <see cref="Stream"/>.
    /// </summary>
    /// <param name="csStream">The stream to write the C# code to.</param>
    public void EmitCSharp(Stream csStream)
    {
        var codegen = new CSharpCodegen(this.GetSemanticModel(this.Parsed!), csStream);
        codegen.Generate();
    }
}
