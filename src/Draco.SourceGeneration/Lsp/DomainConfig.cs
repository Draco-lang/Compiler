using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Draco.SourceGeneration.Lsp;

public sealed class Config
{
    public static Config FromXml(XmlConfig config)
    {
        var basicAssembly = typeof(object).Assembly;
        var builtins = config.BuiltinTypes
            .Select(b => new BuiltinType(b.Name, b.FullName))
            .ToList();
        var generated = config.GeneratedTypes
            .Select(b => new GeneratedType(b.Name))
            .ToList();
        return new(builtins, generated);
    }

    public IList<BuiltinType> BuiltinTypes { get; }
    public IList<GeneratedType> GeneratedTypes { get; }

    public Config(IList<BuiltinType> builtinTypes, IList<GeneratedType> generatedTypes)
    {
        this.BuiltinTypes = builtinTypes;
        this.GeneratedTypes = generatedTypes;
    }
}

public readonly record struct BuiltinType(string Name, string FullName);

public readonly record struct GeneratedType(string DeclaredName);
