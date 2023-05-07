using System.Collections.Generic;
using System.Xml.Serialization;

namespace Draco.SourceGeneration.Lsp;

[XmlRoot(ElementName = "LspModel", Namespace = "http://draco-lang.com/language-server-protocol/model")]
public sealed class XmlConfig
{
    [XmlElement("BuiltinType")]
    public List<XmlBuiltinType> BuiltinTypes { get; set; } = null!;
}

[XmlRoot(ElementName = "BuiltinType")]
public sealed class XmlBuiltinType
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string FullName { get; set; } = string.Empty;
}
