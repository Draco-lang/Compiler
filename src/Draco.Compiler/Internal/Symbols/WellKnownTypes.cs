using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A colelction of well-known types that the compiler needs.
/// </summary>
internal sealed partial class WellKnownTypes
{
    /// <summary>
    /// object.ToString().
    /// </summary>
    public MetadataMethodSymbol SystemObject_ToString => this.object_ToString ??= this.SystemObject
        .Members
        .OfType<MetadataMethodSymbol>()
        .Single(m => m.Name == "ToString");
    private MetadataMethodSymbol? object_ToString;

    /// <summary>
    /// string.Format(string formatString, object[] args).
    /// </summary>
    public MetadataMethodSymbol SystemString_Format => this.systemString_Format ??= this.SystemString
        .Members
        .OfType<MetadataMethodSymbol>()
        .First(m => m.Name == "Format" && m.Parameters is [_, { Type: ArrayTypeSymbol }]);
    private MetadataMethodSymbol? systemString_Format;

    private readonly Compilation compilation;

    public WellKnownTypes(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public MetadataTypeSymbol GetTypeFromAssembly(AssemblyName name, ImmutableArray<string> path)
    {
        var assembly = this.GetAssemblyWithAssemblyName(name);
        return this.GetTypeFromAssembly(assembly, path);
    }

    public MetadataTypeSymbol GetTypeFromAssembly(MetadataAssemblySymbol assembly, ImmutableArray<string> path) =>
        assembly.Lookup(path).OfType<MetadataTypeSymbol>().Single();

    private MetadataAssemblySymbol GetAssemblyWithAssemblyName(AssemblyName name) =>
        this.compilation.MetadataAssemblies.Single(asm => AssemblyNameComparer.Full.Equals(asm.AssemblyName, name));

    private MetadataAssemblySymbol GetAssemblyWithNameAndToken(string name, byte[] token)
    {
        var assemblyName = new AssemblyName() { Name = name };
        assemblyName.SetPublicKeyToken(token);
        return this.compilation.MetadataAssemblies
            .Single(asm => AssemblyNameComparer.NameAndToken.Equals(asm.AssemblyName, assemblyName));
    }
}
