using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;

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

    public override Api.Semantics.Visibility Visibility => this.fieldDefinition.Attributes.HasFlag(FieldAttributes.Public) ? Api.Semantics.Visibility.Public : Api.Semantics.Visibility.Internal;

    public override string Documentation => InterlockedUtils.InitializeNull(ref this.documentation, () => MetadataSymbol.GetDocumentation(this.Assembly, $"F:{this.MetadataFullName}"));
    private string? documentation;

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
