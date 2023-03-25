using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Draco.Lsp.Generation.TypeScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Draco.Lsp.Generation;

internal sealed class Translator
{
    public static string Translate(ModelElement element)
    {
        var stringWriter = new StringWriter();
        var translator = new Translator(stringWriter);
        translator.TranslateModelElement(element);
        var sourceCode = stringWriter.ToString();
        return SyntaxFactory
            .ParseCompilationUnit(sourceCode)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
    }

    private readonly TextWriter writer;

    private Translator(TextWriter writer)
    {
        this.writer = writer;
    }

    private void TranslateModelElement(ModelElement element)
    {
        switch (element)
        {
        case InterfaceModel @interface:
            this.TranslateInterface(@interface);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(element));
        }
    }

    private void TranslateInterface(InterfaceModel @interface)
    {
        this.TranslateDocumentation(@interface.Documentation);
        this.writer.WriteLine($"public sealed class {@interface.Name}");
        this.writer.WriteLine("{");
        foreach (var field in @interface.Fields) this.TranslateField(field);
        this.writer.WriteLine("}");
    }

    private void TranslateField(Field field)
    {
        this.TranslateDocumentation(field.Documentation);
        this.TranslateType(field.Name, field.Type);
        if (field.Nullable) this.writer.Write('?');
        this.writer.Write(' ');
        this.writer.Write(Capitalize(field.Name));
        this.writer.WriteLine(" { get; set; }");
    }

    private void TranslateType(string? fieldName, ModelType type)
    {
        switch (type)
        {
        case NameType name:
        {
            this.writer.Write(name.Name);
            break;
        }
        case ArrayType array:
        {
            this.writer.Write("IList<");
            this.TranslateType(null, array.ElementType);
            this.writer.Write('>');
            break;
        }
        case AnonymousType anonymous:
        {
            Debug.Assert(fieldName is not null);
            var typeName = Capitalize(fieldName);
            this.writer.Write(typeName);
            // TODO: Store anonymous type
            break;
        }
        default:
            throw new ArgumentOutOfRangeException(nameof(type));
        }
    }

    private void TranslateDocumentation(string? docComment)
    {
        if (docComment is null) return;
        this.writer.WriteLine($"""
            /// <summary>
            {string.Join(Environment.NewLine, ExtractDocumentation(docComment).Select(l => $"/// {l}"))}
            /// </summary>
            """);
    }

    private static string[] ExtractDocumentation(string docComment)
    {
        var lines = docComment
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');
        var uncommentedLines = lines[1..^1]
            .Select(line => line[(line.IndexOf('*') + 1)..])
            .Select(line => line.Trim())
            .ToArray();
        return uncommentedLines;
    }

    private static string Capitalize(string text) => $"{char.ToUpper(text[0])}{text[1..]}";
}
