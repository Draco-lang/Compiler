using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.Chr;

[Generator]
public sealed class ChrSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "Chr.xml";
    protected override Type XmlModelType => typeof(XmlConfig);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Config.FromXml((XmlConfig)xmlModel);

        var chrCode = CodeGenerator.GenerateChr(domainModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("Chr.Generated.cs", chrCode),
        };
    }
}
