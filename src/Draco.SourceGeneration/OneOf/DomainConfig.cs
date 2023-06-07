namespace Draco.SourceGeneration.OneOf;

public sealed class Config
{
    public static Config FromXml(XmlConfig config) =>
        new(config.RootNamespace, config.MaxCases);

    public string RootNamespace { get; }
    public int MaxCases { get; }

    public Config(string rootNamespace, int maxCases)
    {
        this.RootNamespace = rootNamespace;
        this.MaxCases = maxCases;
    }
}
