using System.Reflection;
using Draco.Lsp.Generation.CSharp;
using Draco.Lsp.Generation.TypeScript;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Scriban.Runtime;

namespace Draco.Lsp.Generation;

internal class Program
{
    internal static void Main(string[] args)
    {
        var rootPath = @"c:\Development\language-server-protocol\_specifications\lsp\3.17";
        var md = File.ReadAllText(Path.Join(rootPath, "specification.md"));
        md = MarkdownReader.ResolveRelativeIncludes(md, rootPath);
        var tsMerged = string.Join(Environment.NewLine, MarkdownReader.ExtractCodeSnippets(md, "ts", "typescript"));
        var tokens = Lexer.Lex(tsMerged);
        var tsModel = Parser.Parse(tokens);

        var translator = new Translator(tsModel);

        translator.AddBuiltinType("boolean", typeof(bool));
        translator.AddBuiltinType("string", typeof(string));
        translator.AddBuiltinType("integer", typeof(int));
        translator.AddBuiltinType("uinteger", typeof(uint));
        translator.AddBuiltinType("LSPAny", typeof(object));

        translator.GenerateByName("InitializeParams");
        translator.GenerateByName("InitializeResult");
        translator.GenerateByName("InitializedParams");

        var csModel = translator.Generate();
        var csCode = CodeWriter.WriteModel(csModel);

        // Console.WriteLine(csCode);

        Console.WriteLine(RenderOneOf());
    }

    private static string RenderOneOf()
    {
        var template = LoadScribanTemplate("OneOf.sbncs");

        var context = new Scriban.TemplateContext
        {
            MemberRenamer = MemberRenamer,
        };
        var scriptObject = new Scriban.Runtime.ScriptObject();
        scriptObject.Import(new { MaxCases = 8 }, renamer: MemberRenamer);
        context.PushGlobal(scriptObject);

        var output = template.Render(context);
        return SyntaxFactory
            .ParseCompilationUnit(output)
            .NormalizeWhitespace()
            .GetText()
            .ToString();
    }

    private static Scriban.Template LoadScribanTemplate(string templateName)
    {
        var templateString = GetManifestResourceStreamReader(templateName).ReadToEnd();
        return Scriban.Template.Parse(templateString);
    }

    private static StreamReader GetManifestResourceStreamReader(string name)
    {
        name = $"Templates.{name}";
        var assembly = Assembly.GetExecutingAssembly();
        var stream = assembly.GetManifestResourceStream(name)
                  ?? throw new FileNotFoundException($"resource {name} was not embedded in the assembly");
        var reader = new StreamReader(stream);
        return reader;
    }

    private static string MemberRenamer(MemberInfo memberInfo) => memberInfo.Name;
}
