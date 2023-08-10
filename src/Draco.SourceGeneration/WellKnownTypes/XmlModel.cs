using System.Collections.Generic;
using System.Xml.Serialization;

namespace Draco.SourceGeneration.WellKnownTypes;

[XmlRoot(ElementName = "WellKnownTypes", Namespace = "http://draco-lang.com/symbols/well-known-types")]
public sealed class XmlModel
{
    [XmlElement("Assembly")]
    public List<XmlAssembly> Assemblies { get; set; } = null!;

    [XmlElement("Type")]
    public List<XmlType> Types { get; set; } = null!;
}

[XmlRoot(ElementName = "Assembly")]
public sealed class XmlAssembly
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string PublicKeyToken { get; set; } = string.Empty;
}

[XmlRoot(ElementName = "Type")]
public sealed class XmlType
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public string Assembly { get; set; } = string.Empty;

    [XmlElement("Intrinsic")]
    public List<XmlIntrinsic> Intrinsics { get; set; } = null!;
}

[XmlRoot(ElementName = "Intrinsic")]
public sealed class XmlIntrinsic
{
    [XmlAttribute]
    public string Name { get; set; } = string.Empty;

    [XmlAttribute]
    public bool IsValueType { get; set; }
}
