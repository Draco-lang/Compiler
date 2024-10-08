using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.Dap;

[Generator]
public sealed class XmlDapModelSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "DapModel.xml";
    protected override Type XmlModelType => typeof(XmlConfig);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainConfig = Config.FromXml((XmlConfig)xmlModel);

        // Read up the meta-model
        var metaModelJson = EmbeddedResourceLoader.GetManifestResourceStreamReader("Dap", "MetaModel.json").ReadToEnd();

        // Parse the schema
        var schema = JsonDocument.Parse(metaModelJson);

        // Create translator
        var translator = new Translator(schema);

        // Configure translator
        foreach (var (name, fullName) in domainConfig.BuiltinTypes) translator.AddBuiltinType(name, fullName);

        // Translate
        var csModel = translator.Translate();

        // Finally generate by template
        var dapModelCode = Template.Generate(csModel);

        return
        [
            new("DapModel.Generated.cs", dapModelCode),
        ];
    }
}
