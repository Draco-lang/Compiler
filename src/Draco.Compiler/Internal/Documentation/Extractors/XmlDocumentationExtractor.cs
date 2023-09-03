using System.Collections.Immutable;
using System.Linq;
using System.Xml;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Metadata;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Documentation.Extractors;

/// <summary>
/// Extracts XML into <see cref="SymbolDocumentation"/>.
/// </summary>
internal sealed class XmlDocumentationExtractor
{
    /// <summary>
    /// Extracts the xml documentation from <paramref name="containingSymbol"/>.
    /// </summary>
    /// <returns>The extracted XMl as <see cref="SymbolDocumentation"/>.</returns>
    public static SymbolDocumentation Extract(Symbol containingSymbol) =>
        new XmlDocumentationExtractor(containingSymbol.RawDocumentation, containingSymbol).Extract();

    private readonly string xml;
    private readonly Symbol containingSymbol;
    private MetadataAssemblySymbol Assembly => this.containingSymbol.AncestorChain.OfType<MetadataAssemblySymbol>().First();

    private XmlDocumentationExtractor(string xml, Symbol containingSymbol)
    {
        this.xml = xml;
        this.containingSymbol = containingSymbol;
    }

    /// <summary>
    /// Extracts the <see cref="xml"/>.
    /// </summary>
    /// <returns>The extracted XMl as <see cref="SymbolDocumentation"/>.</returns>
    private SymbolDocumentation Extract()
    {
        // TODO: exception
        //       para
        //       list
        //       c
        //       see - not cref links
        //       seealso
        //       b ?
        //       i ?

        var xml = $"""
            <documentation>
              {this.xml}
            </documentation>
            """;
        var doc = new XmlDocument();
        doc.LoadXml(xml);

        var raw = doc.DocumentElement!.ChildNodes
            .Cast<XmlNode>()
            .Select(this.ExtractSectionOrElement);

        var sections = raw.OfType<DocumentationSection>().ToList();
        var elements = raw.OfType<DocumentationElement>();

        foreach (var grouped in elements.GroupBy(x => x.GetType()))
        {
            if (grouped.Key == typeof(ParameterDocumentationElement)) sections.Add(new DocumentationSection(SectionKind.Parameters, grouped.ToImmutableArray()));
            else if (grouped.Key == typeof(TypeParameterDocumentationElement)) sections.Add(new DocumentationSection(SectionKind.TypeParameters, grouped.ToImmutableArray()));
        }
        return new SymbolDocumentation(sections.ToImmutableArray());
    }

    private object ExtractSectionOrElement(XmlNode node) => node.Name switch
    {
        "param" => new ParameterDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty), this.ExtractElementsFromNode(node)),
        "typeparam" => new TypeParameterDocumentationElement(this.GetTypeParameter(node.Attributes?["name"]?.Value ?? string.Empty), this.ExtractElementsFromNode(node)),
        "code" => new DocumentationSection(SectionKind.Code, ImmutableArray.Create(this.ExtractElement(node))),
        "summary" => new DocumentationSection(SectionKind.Summary, this.ExtractElementsFromNode(node)),
        _ => new DocumentationSection(node.Name, this.ExtractElementsFromNode(node)),
    };

    private DocumentationElement ExtractElement(XmlNode node) => node.LocalName switch
    {
        "#text" => new TextDocumentationElement(node.InnerText),
        "see" => this.ConstructReference(node),
        "paramref" => new ReferenceDocumentationElement(this.GetParameter(node.Attributes?["name"]?.Value ?? string.Empty)),
        "typeparamref" => new ReferenceDocumentationElement(this.GetTypeParameter(node.Attributes?["name"]?.Value ?? string.Empty)),
        "code" => new CodeDocumentationElement(node.InnerXml.Trim('\r', '\n'), "cs"),
        _ => new TextDocumentationElement(node.InnerText),
    };

    private ImmutableArray<DocumentationElement> ExtractElementsFromNode(XmlNode node)
    {
        var elements = ImmutableArray.CreateBuilder<DocumentationElement>();
        foreach (XmlNode child in node.ChildNodes) elements.Add(this.ExtractElement(child));
        return elements.ToImmutable();
    }

    private ReferenceDocumentationElement ConstructReference(XmlNode node)
    {
        var cref = node.Attributes?["cref"]?.Value;
        var symbol = this.GetSymbolFromDocumentationName(cref ?? string.Empty)
            // NOTE: The first two characters of the link is the documentation prefix
            ?? new PrimitiveTypeSymbol(cref?[2..] ?? string.Empty, false);
        return new ReferenceDocumentationElement(symbol, string.IsNullOrEmpty(node.InnerText) ? null : node.InnerText);
    }

    private Symbol? GetSymbolFromDocumentationName(string documentationName) =>
        this.Assembly.Compilation.MetadataAssemblies.Values
            .Select(x => x.RootNamespace.LookupByPrefixedDocumentationName(documentationName))
            .OfType<Symbol>()
            .FirstOrDefault();

    private ParameterSymbol? GetParameter(string paramName) =>
        (this.containingSymbol as FunctionSymbol)?.Parameters.FirstOrDefault(x => x.Name == paramName);

    private TypeParameterSymbol? GetTypeParameter(string paramName) =>
        (this.containingSymbol as FunctionSymbol)?.GenericParameters.FirstOrDefault(x => x.Name == paramName);
}
