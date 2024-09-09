using static Draco.SourceGeneration.TemplateUtils;

namespace Draco.SourceGeneration.WellKnownTypes;

internal static class Template
{
    public static string Generate(WellKnownTypes config) => FormatCSharp($$"""
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;

#nullable enable

namespace Draco.Compiler.Internal.Symbols;

internal sealed partial class WellKnownTypes
{
    {{ForEach(config.Assemblies, assembly => $$"""
        private static byte[] {{PublicKeyTokenName(assembly)}} { get; } = new byte[]
        {
            {{ForEach(assembly.PublicKeyToken, ", ", b => $"0x{b:X2}")}}
        };

        public MetadataAssemblySymbol {{AssemblyName(assembly)}} => LazyInitializer.EnsureInitialized(
            ref this.{{CamelCase(AssemblyName(assembly))}},
            () => this.GetAssemblyWithNameAndToken("{{assembly.Name}}", {{PublicKeyTokenName(assembly)}}));
        private MetadataAssemblySymbol? {{CamelCase(AssemblyName(assembly))}};
    """)}}

    {{ForEach(config.Types, type => $$"""
        public MetadataTypeSymbol {{TypeName(type)}} => LazyInitializer.EnsureInitialized(
            ref this.{{CamelCase(TypeName(type))}},
            () => this.GetTypeFromAssembly(
                {{AssemblyName(type.Assembly)}},
                [{{ForEach(type.Name.Split('.'), ", ", e => $"\"{e}\"")}}]));
        private MetadataTypeSymbol? {{CamelCase(TypeName(type))}};
    """)}}
}

#nullable restore
""");

    private static string PublicKeyTokenName(WellKnownAssembly assembly) => $"{TypeToIdentifier(assembly.Name)}_PublicKeyToken";
    private static string AssemblyName(WellKnownAssembly assembly) => TypeToIdentifier(assembly.Name);
    private static string TypeName(WellKnownType type) => TypeToIdentifier(type.Name);

    private static string TypeToIdentifier(string name) => name
        .Replace(".", string.Empty)
        .Replace('`', '_');
}
