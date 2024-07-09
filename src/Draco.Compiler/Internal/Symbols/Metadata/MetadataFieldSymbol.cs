using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;
using Draco.Compiler.Internal.Documentation;
using Draco.Compiler.Internal.Documentation.Extractors;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Nonstatic fields read from metadata.
/// </summary>
internal sealed class MetadataFieldSymbol : FieldSymbol, IMetadataSymbol
{
    public override TypeSymbol Type => LazyInitializer.EnsureInitialized(ref this.type, this.BuildType);
    private TypeSymbol? type;

    public override bool IsMutable => !(this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Literal) || this.fieldDefinition.Attributes.HasFlag(FieldAttributes.InitOnly));

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

    public override SymbolDocumentation Documentation => LazyInitializer.EnsureInitialized(ref this.documentation, this.BuildDocumentation);
    private SymbolDocumentation? documentation;

    internal override string RawDocumentation => LazyInitializer.EnsureInitialized(ref this.rawDocumentation, this.BuildRawDocumentation);
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
        if (fieldDefinition.Attributes.HasFlag(FieldAttributes.Static))
        {
            throw new System.ArgumentException("fields must be constructed from nonstatic fields");
        }

        this.ContainingSymbol = containingSymbol;
        this.fieldDefinition = fieldDefinition;
    }

    private TypeSymbol BuildType() =>
        this.fieldDefinition.DecodeSignature(this.Assembly.Compilation.TypeProvider, this);

    private SymbolDocumentation BuildDocumentation() =>
        XmlDocumentationExtractor.Extract(this);

    private string BuildRawDocumentation() =>
        MetadataSymbol.GetDocumentation(this);
}
