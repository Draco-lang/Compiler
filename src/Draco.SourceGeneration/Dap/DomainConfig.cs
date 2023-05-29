using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.Dap;

public sealed class Config
{
    public static Config FromXml(XmlConfig config)
    {
        var basicAssembly = typeof(object).Assembly;
        var builtins = config.BuiltinTypes
            .Select(b => new BuiltinType(b.Name, b.FullName))
            .ToList();
        return new(builtins);
    }

    public IList<BuiltinType> BuiltinTypes { get; }

    public Config(IList<BuiltinType> builtinTypes)
    {
        this.BuiltinTypes = builtinTypes;
    }
}

public readonly record struct BuiltinType(string Name, string FullName);
