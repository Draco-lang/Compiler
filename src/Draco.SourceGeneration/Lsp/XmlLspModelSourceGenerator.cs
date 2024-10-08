using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.Lsp;

[Generator]
public sealed class XmlLspModelSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "LspModel.xml";
    protected override Type XmlModelType => typeof(XmlConfig);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainConfig = Config.FromXml((XmlConfig)xmlModel);

        // Read up the meta-model
        var metaModelJson = EmbeddedResourceLoader.GetManifestResourceStreamReader("Lsp", "MetaModel.json").ReadToEnd();

        // Deserialize it
        var jsonOptions = new JsonSerializerOptions();
        jsonOptions.Converters.Add(new Metamodel.TypeConverter());
        jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        jsonOptions.PropertyNameCaseInsensitive = true;
        var metaModel = JsonSerializer.Deserialize<Metamodel.MetaModel>(metaModelJson, jsonOptions)!;

        // Create translator
        var translator = new Translator(metaModel);

        // Configure translator
        foreach (var (name, fullName) in domainConfig.BuiltinTypes) translator.AddBuiltinType(name, fullName);

        // Translate
        var csModel = translator.Translate();

        // Finally generate by template
        var lspModelCode = Template.Generate(csModel);

        return
        [
            new("LspModel.Generated.cs", lspModelCode),
        ];
    }
}
