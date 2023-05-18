using Draco.Compiler.Api.Syntax;

namespace Draco.Compiler.Tests;

internal static class ModuleTestsUtilities
{
    public static string ToPath(params string[] parts) => Path.GetFullPath(Path.Combine(parts));
}
