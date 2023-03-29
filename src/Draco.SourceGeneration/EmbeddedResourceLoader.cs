using System.IO;
using System.Reflection;

namespace Draco.SourceGeneration;

public static class EmbeddedResourceLoader
{
    public static StreamReader GetManifestResourceStreamReader(string prefix, string name)
    {
        var fullName = $"{prefix}.{name}";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(fullName)
                  ?? throw new FileNotFoundException($"resource {fullName} was not embedded in the assembly");
        var reader = new StreamReader(stream);
        return reader;
    }
}
