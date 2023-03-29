using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using System.Threading;
using System.IO;

namespace Draco.SourceGeneration.Lsp;

[Generator]
public sealed class XmlLspModelSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "LspModel.xml";
    protected override Type XmlModelType => typeof(XmlConfig);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainConfig = Config.FromXml((XmlConfig)xmlModel);

        // Read up markdown
        var md = EmbeddedResourceLoader.GetManifestResourceStreamReader("Lsp", "specification.md").ReadToEnd();
        // Merge them by includes
        md = MarkdownProcessor.ResolveRelativeIncludes(md, string.Empty);
        // Merge TS snippets
        var tsMerged = string.Join("\n", MarkdownProcessor.ExtractCodeSnippets(md, "ts", "typescript"));
        // Parse TS code
        var tokens = TypeScript.Lexer.Lex(tsMerged);
        var tsModel = TypeScript.Parser.Parse(tokens);

        // Create translator
        var translator = new Translator(tsModel);

        // Configure translator
        foreach (var (name, fullName) in domainConfig.BuiltinTypes) translator.AddBuiltinType(name, fullName);
        foreach (var gen in domainConfig.GeneratedTypes) translator.GenerateByName(gen.DeclaredName);

        // Translate
        var csModel = translator.Generate();

        // Finally generate by template
        var lspModelCode = CodeGenerator.GenerateLspModel(csModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("LspModel.Generated.cs", lspModelCode),
        };
    }
}
