using System.Xml.Serialization;

namespace Draco.SourceGeneration.OneOf;

[XmlRoot(ElementName = "OneOf", Namespace = "http://draco-lang.com/one-of")]
public sealed class XmlConfig
{
    [XmlAttribute]
    public int MaxCases { get; set; }
}
