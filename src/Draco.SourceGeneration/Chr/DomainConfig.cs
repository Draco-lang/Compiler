namespace Draco.SourceGeneration.Chr;

public sealed class Config
{
    public static Config FromXml(XmlConfig config) =>
        new(config.MaxCases);

    public int MaxCases { get; }

    public Config(int maxCases)
    {
        this.MaxCases = maxCases;
    }
}
