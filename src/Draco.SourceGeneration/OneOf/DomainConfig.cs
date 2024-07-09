namespace Draco.SourceGeneration.OneOf;

public sealed class Config(string rootNamespace, int maxCases)
{
    public static Config FromXml(XmlConfig config) =>
        new(config.RootNamespace, config.MaxCases);

    public string RootNamespace { get; } = rootNamespace;
    public int MaxCases { get; } = maxCases;
}
