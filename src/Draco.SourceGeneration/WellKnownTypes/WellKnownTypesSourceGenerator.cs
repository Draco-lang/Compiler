using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.WellKnownTypes;

[Generator]
public sealed class WellKnownTypesSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "WellKnownTypes.xml";
    protected override Type XmlModelType => typeof(XmlModel);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = WellKnownTypes.FromXml((XmlModel)xmlModel);

        var wellKnownTypesCode = CodeGenerator.GenerateWellKnownTypes(domainModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("WellKnownTypes.Generated.cs", wellKnownTypesCode),
        };
    }
}
