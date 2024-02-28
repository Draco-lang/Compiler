using System.Xml.Serialization;

namespace Draco.SourceGeneration.Chr;

[XmlRoot(ElementName = "Chr", Namespace = "http://draco-lang.com/chr")]
public sealed class XmlConfig
{
    [XmlAttribute]
    public int MaxCases { get; set; }
}
