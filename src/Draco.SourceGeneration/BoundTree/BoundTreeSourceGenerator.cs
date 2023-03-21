using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.BoundTree;

[Generator]
public sealed class BoundTreeSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "BoundNodes.xml";

    protected override Type XmlModelType => typeof(XmlTree);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Tree.FromXml((XmlTree)xmlModel);

        var boundTreeCode = CodeGenerator.GenerateBoundTree(domainModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("BoundTree.Generated.cs", boundTreeCode),
        };
    }
}
