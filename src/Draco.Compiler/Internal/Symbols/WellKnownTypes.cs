using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Generic;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A collection of well-known types that the compiler needs.
/// </summary>
internal sealed partial class WellKnownTypes
{
    /// <summary>
    /// object.ToString().
    /// </summary>
    public MetadataMethodSymbol SystemObject_ToString => InterlockedUtils.InitializeNull(
        ref this.object_ToString,
        () => this.SystemObject
            .Members
            .OfType<MetadataMethodSymbol>()
            .Single(m => m.Name == "ToString"));
    private MetadataMethodSymbol? object_ToString;

    /// <summary>
    /// string.Format(string formatString, object[] args).
    /// </summary>
    public MetadataMethodSymbol SystemString_Format => InterlockedUtils.InitializeNull(
        ref this.systemString_Format,
        () => this.SystemString
            .Members
            .OfType<MetadataMethodSymbol>()
            .First(m =>
                m.Name == "Format"
             && m.Parameters is [_, { Type: TypeInstanceSymbol { GenericDefinition: ArrayTypeSymbol } }]));
    private MetadataMethodSymbol? systemString_Format;

    private readonly Compilation compilation;

    public WellKnownTypes(Compilation compilation)
    {
        this.compilation = compilation;
    }

    public MetadataTypeSymbol GetTypeFromAssembly(AssemblyName name, ImmutableArray<string> path)
    {
        using var _ = this.compilation.Tracer.Begin(
            parameters: ImmutableArray.Create<object?>(name, string.Join(".", path)));

        var assembly = this.GetAssemblyWithAssemblyName(name);
        return this.GetTypeFromAssembly(assembly, path);
    }

    public MetadataTypeSymbol GetTypeFromAssembly(MetadataAssemblySymbol assembly, ImmutableArray<string> path)
    {
        using var _ = this.compilation.Tracer.Begin(
            parameters: ImmutableArray.Create<object?>(assembly.Name, string.Join(".", path)));

        return assembly.Lookup(path).OfType<MetadataTypeSymbol>().Single();
    }

    private MetadataAssemblySymbol GetAssemblyWithAssemblyName(AssemblyName name) =>
        this.compilation.MetadataAssemblies.Values.Single(asm => AssemblyNameComparer.Full.Equals(asm.AssemblyName, name));

    private MetadataAssemblySymbol GetAssemblyWithNameAndToken(string name, byte[] token)
    {
        using var _ = this.compilation.Tracer.Begin(
            parameters: ImmutableArray.Create<object?>(name));

        var assemblyName = new AssemblyName() { Name = name };
        assemblyName.SetPublicKeyToken(token);
        return this.compilation.MetadataAssemblies.Values
            .Single(asm => AssemblyNameComparer.NameAndToken.Equals(asm.AssemblyName, assemblyName));
    }
}
