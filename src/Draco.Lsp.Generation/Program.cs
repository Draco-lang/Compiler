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
        translator.AddBuiltinType("integer", typeof(int));
        translator.AddBuiltinType("uinteger", typeof(uint));
        translator.AddBuiltinType("LSPAny", typeof(object));

        translator.GenerateByName("InitializeParams");
        translator.GenerateByName("InitializeResult");
        translator.GenerateByName("InitializedParams");

        var csModel = translator.Generate();
        var csCode = CodeWriter.WriteModel(csModel);

        Console.WriteLine(csCode);
    }
}
