using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests;

internal static class ModuleTestsUtilities
{
    public static SyntaxTree CreateSyntaxTree(string source, string path) =>
        SyntaxTree.Parse(SourceText.FromText(new Uri(path), source.AsMemory()));

    public static string ToPath(params string[] parts) => Path.Combine(parts);
}
