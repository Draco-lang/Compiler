using System;
using System.Collections.Generic;
using System.Linq;

namespace Draco.SourceGeneration.WellKnownTypes;

public sealed class WellKnownTypes(
    IList<WellKnownAssembly> assemblies,
    IList<WellKnownType> types)
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
            var wellKnownType = new WellKnownType(name, assembly);
            types.Add(wellKnownType);
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

    public IList<WellKnownAssembly> Assemblies { get; } = assemblies;
    public IList<WellKnownType> Types { get; } = types;
}

public sealed class WellKnownAssembly(string name, byte[] publicKeyToken)
{
    public string Name { get; } = name;
    public byte[] PublicKeyToken { get; } = publicKeyToken;
}

public sealed class WellKnownType(string name, WellKnownAssembly assembly)
{
    public string Name { get; } = name;
    public WellKnownAssembly Assembly { get; } = assembly;
}
