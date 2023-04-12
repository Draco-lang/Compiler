using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Metadata;

namespace Draco.Compiler.Internal.Symbols;

/// <summary>
/// A colelction of well-known types that the compiler needs.
/// </summary>
internal sealed class WellKnownTypes
{
    /// <summary>
    /// The public-key token of Microsoft assemblies.
    /// </summary>
    public static byte[] MicrosoftPublicKeyToken { get; } = new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a };

    /// <summary>
    /// The System.Runtime assembly referenced by the compilation.
    /// </summary>
    public MetadataAssemblySymbol SystemRuntime => this.systemRuntime ??= this.GetAssemblyWithNameAndToken("System.Runtime", MicrosoftPublicKeyToken);
    private MetadataAssemblySymbol? systemRuntime;

    /// <summary>
    /// System.Object inside System.Runtime.
    /// </summary>
    public MetadataTypeSymbol SystemObject => this.systemObject ??= this.SystemRuntime
        .Lookup(ImmutableArray.Create("System", "Object"))
        .OfType<MetadataTypeSymbol>()
        .First();
    private MetadataTypeSymbol? systemObject;

    /// <summary>
    /// System.Int32 inside System.Runtime.
    /// </summary>
    public MetadataTypeSymbol SystemInt32 => this.systemInt32 ??= this.SystemRuntime
        .Lookup(ImmutableArray.Create("System", "Int32"))
        .OfType<MetadataTypeSymbol>()
        .First();
    private MetadataTypeSymbol? systemInt32;

    /// <summary>
    /// System.Array inside System.Runtime.
    /// </summary>
    public MetadataTypeSymbol SystemArray => this.systemArray ??= this.SystemRuntime
        .Lookup(ImmutableArray.Create("System", "Array"))
        .OfType<MetadataTypeSymbol>()
        .First();
    private MetadataTypeSymbol? systemArray;

    /// <summary>
    /// object.ToString().
    /// </summary>
    public MetadataMethodSymbol SystemObject_ToString => this.object_ToString ??= this.SystemObject
        .Members
        .OfType<MetadataMethodSymbol>()
        .Single(m => m.Name == "ToString");
    private MetadataMethodSymbol? object_ToString;

    /// <summary>
    /// System.String inside System.Runtime.
    /// </summary>
    public MetadataTypeSymbol SystemString => this.systemString ??= this.SystemRuntime
        .Lookup(ImmutableArray.Create("System", "String"))
        .OfType<MetadataTypeSymbol>()
        .First();
    private MetadataTypeSymbol? systemString;

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

    private MetadataAssemblySymbol GetAssemblyWithNameAndToken(string name, byte[] token)
    {
        var assemblyName = new AssemblyName() { Name = name };
        assemblyName.SetPublicKeyToken(token);
        return this.compilation.MetadataAssemblies
            .Single(asm => AssemblyNameComparer.NameAndToken.Equals(asm.AssemblyName, assemblyName));
    }
}
