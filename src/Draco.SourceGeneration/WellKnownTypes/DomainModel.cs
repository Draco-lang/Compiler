using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.WellKnownTypes;

public sealed class WellKnownTypes
{
    public static WellKnownTypes FromXml(XmlModel model)
    {
        var assemblies = new List<WellKnownAssembly>();
        var types = new List<WellKnownType>();

        foreach (var assembly in model.Assemblies)
        {
            var name = assembly.Name;
            var pkToken = HexStringToBytes(assembly.PublicKeyToken);
            assemblies.Add(new(name, pkToken));
        }

        foreach (var type in model.Types)
        {
            var name = type.Name;
            var assembly = assemblies.FirstOrDefault(a => a.Name == type.Assembly)
                        ?? throw new InvalidOperationException($"well-known type {name} references assembly {type.Assembly}, which is not found");
            types.Add(new(name, assembly));
        }

        return new(assemblies, types);
    }

    private static byte[] HexStringToBytes(string hex)
    {
        var result = new byte[hex.Length / 2];
        for (var i = 0; i < hex.Length; i += 2)
        {
            var b = HexCharToInt(hex[i]) * 16 + HexCharToInt(hex[i + 1]);
            result[i / 2] = (byte)b;
        }
        return result;
    }

    private static int HexCharToInt(char ch) => ch switch
    {
        >= '0' and <= '9' => ch - '0',
        >= 'A' and <= 'F' => ch - 'A' + 10,
        >= 'a' and <= 'f' => ch - 'a' + 10,
        _ => throw new ArgumentOutOfRangeException(nameof(ch)),
    };

    public IList<WellKnownAssembly> Assemblies { get; }
    public IList<WellKnownType> Types { get; }

    public WellKnownTypes(
        IList<WellKnownAssembly> assemblies,
        IList<WellKnownType> types)
    {
        this.Assemblies = assemblies;
        this.Types = types;
    }
}

public sealed class WellKnownAssembly
{
    public string Name { get; }
    public byte[] PublicKeyToken { get; }

    public WellKnownAssembly(string name, byte[] publicKeyToken)
    {
        this.Name = name;
        this.PublicKeyToken = publicKeyToken;
    }
}

public sealed class WellKnownType
{
    public string Name { get; }
    public WellKnownAssembly Assembly { get; }

    public WellKnownType(string name, WellKnownAssembly assembly)
    {
        this.Name = name;
        this.Assembly = assembly;
    }
}
