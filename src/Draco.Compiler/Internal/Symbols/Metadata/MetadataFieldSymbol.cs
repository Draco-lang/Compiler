using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Fields read from metadata.
/// </summary>
internal sealed class MetadataFieldSymbol : FieldSymbol, IMetadataSymbol
{
    public override TypeSymbol Type => InterlockedUtils.InitializeNull(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override bool IsMutable => !(this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Literal) || this.fieldDefinition.Attributes.HasFlag(FieldAttributes.InitOnly));

    public override bool IsStatic => this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Static);

    public override string Name => this.MetadataReader.GetString(this.fieldDefinition.Name);

    public override Api.Semantics.Visibility Visibility
    {
        get
        {
            // If this is an interface member, default to public
            if (this.ContainingSymbol is TypeSymbol { IsInterface: true })
            {
                return Api.Semantics.Visibility.Public;
            }

            // Otherwise read flag from metadata
            return this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Public)
                ? Api.Semantics.Visibility.Public
                : Api.Semantics.Visibility.Internal;
        }
    }

    public override SymbolDocumentation Documentation => InterlockedUtils.InitializeNull(ref this.documentation, () => new XmlDocumentationExtractor(this.RawDocumentation, this).Extract());
    private SymbolDocumentation? documentation;

    public override string RawDocumentation => InterlockedUtils.InitializeNull(ref this.rawDocumentation, () => MetadataSymbol.GetDocumentation(this.Assembly, $"F:{this.DocumentationFullName}"));
    private string? rawDocumentation;

    public override Symbol? ContainingSymbol { get; }

    /// <summary>
    /// The metadata assembly of this metadata symbol.
    /// </summary>
    // NOTE: thread-safety does not matter, same instance
    public MetadataAssemblySymbol Assembly => this.assembly ??= this.AncestorChain.OfType<MetadataAssemblySymbol>().First();
    private MetadataAssemblySymbol? assembly;

    /// <summary>
    /// The metadata reader that was used to read up this metadata symbol.
    /// </summary>
    public MetadataReader MetadataReader => this.Assembly.MetadataReader;

    private readonly FieldDefinition fieldDefinition;

    public MetadataFieldSymbol(Symbol containingSymbol, FieldDefinition fieldDefinition)
    {
        this.ContainingSymbol = containingSymbol;
        this.fieldDefinition = fieldDefinition;
    }

    private TypeSymbol BuildType()
    {
        // Decode signature
        var decoder = new TypeProvider(this.Assembly.Compilation);
        return this.fieldDefinition.DecodeSignature(decoder, this);
    }
}
