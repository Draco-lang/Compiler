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

namespace Draco.Compiler.Internal.Symbols;

#nullable enable

internal sealed partial class WellKnownTypes
{
    {{ForEach(config.Assemblies, assembly => $$"""
        private static byte[] {{PublicKeyTokenName(assembly)}} { get; } = new byte[]
        {
            {{ForEach(assembly.PublicKeyToken, ", ", b => $"0x{b:X2}")}}
        };

        public MetadataAssemblySymbol {{PropertyName(assembly)}} => LazyInitializer.EnsureInitialized(
            ref this.{{BackingFieldName(assembly)}},
            () => this.GetAssemblyWithNameAndToken("{{assembly.Name}}", {{PublicKeyTokenName(assembly)}}));
        private MetadataAssemblySymbol? {{BackingFieldName(assembly)}};
    """)}}

    {{ForEach(config.Types, type => $$"""
        public MetadataTypeSymbol {{PropertyName(type)}} => LazyInitializer.EnsureInitialized(
            ref this.{{BackingFieldName(type)}},
            () => this.GetTypeFromAssembly({{PropertyName(type.Assembly)}}, [{{ForEach(type.Name.Split('.'), ", ", StringLiteral)}}]));
        private MetadataTypeSymbol? {{BackingFieldName(type)}};
    """)}}
}

#nullable restore
""");

    private static string PublicKeyTokenName(WellKnownAssembly assembly) => $"{TypeToIdentifier(assembly.Name)}_PublicKeyToken";
    private static string PropertyName(WellKnownAssembly assembly) => TypeToIdentifier(assembly.Name);
    private static string PropertyName(WellKnownType type) => TypeToIdentifier(type.Name);
    private static string BackingFieldName(WellKnownAssembly assembly) => CamelCase(PropertyName(assembly));
    private static string BackingFieldName(WellKnownType type) => CamelCase(PropertyName(type));

    private static string TypeToIdentifier(string name) => name
        .Replace(".", string.Empty)
        .Replace('`', '_');
}
