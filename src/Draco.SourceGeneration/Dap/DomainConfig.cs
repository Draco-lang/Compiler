using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.Dap;

public sealed class Config
{
    public static Config FromXml(XmlConfig config)
    {
        return new();
    }

    public Config()
    {
    }
}
