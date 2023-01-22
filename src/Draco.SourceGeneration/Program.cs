using System;
using System.IO;
using System.Xml.Serialization;
using Draco.SourceGeneration.SyntaxTree;
using Scriban;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Scriban.Runtime;
using Scriban.Parsing;
using System.Threading.Tasks;

namespace Draco.SourceGeneration;

public sealed class ScribanHelperFunctions : ScriptObject
{
    public static string ToCamelCase(string str)
    {
        if (str.Length == 0) return str;
        return $"{char.ToLower(str[0])}{str[1..]}";
    }
}

public sealed class ScribanTemplateLoader : ITemplateLoader
{
    public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) =>
        Path.GetFullPath(templateName);
    public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        // TODO
        File.ReadAllText("../../../SyntaxTree/Common.sbncs");
    public async ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) =>
        await File.ReadAllTextAsync(templatePath);
}

internal class Program
{
    internal static void Main(string[] args)
    {
        var syntaxXml = File.ReadAllText("../../../../Draco.Compiler/Internal/Syntax/Syntax.xml");
        var str = new StringWriter();
        var serializer = new XmlSerializer(typeof(XmlTree));
        var xmlModel = (XmlTree)serializer.Deserialize(new StringReader(syntaxXml))!;
        var domainModel = Tree.FromXml(xmlModel);

        var template = Template.Parse(File.ReadAllText("../../../SyntaxTree/GreenTree.sbncs"));

        var context = new TemplateContext();
        context.TemplateLoader = new ScribanTemplateLoader();
        var scriptObject = new ScriptObject();
        scriptObject.Import(new ScribanHelperFunctions());
        scriptObject.Import(domainModel, renamer: m => m.Name);
        context.PushGlobal(scriptObject);
        context.MemberRenamer = m => m.Name;

        var output = template.Render(context);
        output = SyntaxFactory
            .ParseCompilationUnit(output)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
        Console.WriteLine(output);
    }
}
