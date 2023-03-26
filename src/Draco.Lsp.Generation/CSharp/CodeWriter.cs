using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Lsp.Generation.TypeScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.Lsp.Generation.CSharp;

internal static class CodeWriter
{
    private static readonly string doubleNewline = string.Concat(Environment.NewLine, Environment.NewLine);

    public static string WriteModel(Model model)
    {
        var result = string.Join(doubleNewline, model.Declarations.Select(WriteDeclaration));
        return SyntaxFactory
            .ParseCompilationUnit(result)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
    }

    private static string WriteDeclaration(Declaration declaration) => declaration switch
    {
        Class @class => WriteClass(@class),
        Interface @interface => WriteInterface(@interface),
        Enum @enum => WriteEnum(@enum),
        _ => throw new ArgumentOutOfRangeException(nameof(declaration)),
    };

    private static string WriteClass(Class @class) =>
        $$"""
        {{WriteDocumentation(@class.Documentation)}}
        public sealed class {{@class.Name}} {{WriteInterfaces(@class.Interfaces)}}
        {
            {{string.Join(doubleNewline, @class.NestedDeclarations.Select(WriteDeclaration))}}

            {{string.Join(doubleNewline, @class.Properties.Select(WriteProperty))}}
        }
        """;

    private static string WriteInterface(Interface @interface) =>
        $$"""
        {{WriteDocumentation(@interface.Documentation)}}
        public interface {{@interface.Name}} {{WriteInterfaces(@interface.Interfaces)}}
        {
            {{string.Join(doubleNewline, @interface.Properties.Select(WriteProperty))}}
        }
        """;

    private static string WriteEnum(Enum @enum) =>
        $$"""
        {{WriteDocumentation(@enum.Documentation)}}
        public enum {{@enum.Name}}
        {
            {{string.Join(doubleNewline, @enum.Members.Select(WriteEnumMember))}}
        }
        """;

    private static object WriteEnumMember(EnumMember member) =>
        $"""
        {WriteDocumentation(member.Documentation)}
        {member.Name},
        """;

    private static string WriteProperty(Property prop) =>
        $$"""
        {{WriteDocumentation(prop.Documentation)}}
        public {{WriteType(prop.Type)}} {{prop.Name}} { get; set; }
        """;

    private static string WriteType(Type type) => type switch
    {
        DeclarationType decl => decl.Declaration.Name,
        DiscriminatedUnionType du => $"OneOf<{string.Join(", ", du.Alternatives.Select(WriteType))}>",
        BuiltinType b => b.Type.Name,
        ArrayType a => $"IList<{WriteType(a.ElementType)}>",
        NullableType n => $"{WriteType(n.Type)}?",
        _ => throw new ArgumentOutOfRangeException(nameof(type)),
    };

    private static string WriteInterfaces(IEnumerable<Interface> interfaces) => interfaces.Any()
        ? $": {string.Join(", ", interfaces.Select(i => i.Name))}"
        : string.Empty;

    private static string WriteDocumentation(string? doc)
    {
        if (doc is null) return string.Empty;
        var lines = doc
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');
        return $"""
            /// <summary>
            {string.Join(Environment.NewLine, lines.Select(l => $"/// {l}"))}
            /// </summary>
            """;
    }
}
