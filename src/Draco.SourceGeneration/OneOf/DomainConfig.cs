using System;
using System.Collections.Generic;
using System.Text;

namespace Draco.SourceGeneration.OneOf;

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
