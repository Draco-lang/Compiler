using System.Collections.Immutable;
using System.Xml;

namespace Draco.Compiler.Internal.Documentation;

internal static class XmlDocumentationExtractor
{
    public static SymbolDocumentation Extract(string xml)
    {
        var doc = new XmlDocument();
        doc.LoadXml(xml);
        return new SymbolDocumentation(ImmutableArray<DocumentationSection>.Empty);
    }
}
