using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Draco.Compiler.Tests.EndToEnd;

public sealed class MetadataTests : EndToEndTestsBase
{
    [Fact]
    public void TestReferencedAssemblyVersion()
    {
        using var assemblyBytes = CompileRaw("""
            import System.Console;
            func main() {
                WriteLine();
            }
            """);

        using var peReader = new PEReader(assemblyBytes);
        var metadataReader = peReader.GetMetadataReader();

        var systemConsole = new AssemblyName("System.Console")
        {
            Version = new Version(8, 0, 0, 0)
        };

        Assert.Contains(metadataReader.AssemblyReferences, r =>
        {
            var reference = metadataReader.GetAssemblyReference(r);
            var refName = reference.GetAssemblyName();
            return refName.Name == systemConsole.Name
                && refName.Version == systemConsole.Version;
        });
    }
}
