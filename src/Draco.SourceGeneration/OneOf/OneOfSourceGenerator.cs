using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.OneOf;

[Generator]
public sealed class OneOfSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "OneOf.xml";
    protected override Type XmlModelType => typeof(XmlConfig);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Config.FromXml((XmlConfig)xmlModel);

        var oneOfCode = Template.Generate(domainModel);

        return
        [
            new("OneOf.Generated.cs", oneOfCode),
        ];
    }
}
