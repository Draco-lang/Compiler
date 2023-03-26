using Draco.Lsp.Generation.CSharp;
using Draco.Lsp.Generation.TypeScript;

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
        translator.AddBuiltinType("LSPAny", typeof(object));
        translator.GenerateByName("ServerCapabilities");
        var csModel = translator.Generate();

        Console.WriteLine(CodeWriter.WriteModel(csModel));
    }
}
