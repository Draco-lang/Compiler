using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.UntypedTree;

[Generator]
public sealed class UntypedTreeSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "UntypedNodes.xml";

    protected override Type XmlModelType => typeof(XmlTree);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Tree.FromXml((XmlTree)xmlModel);

        var untypedTreeCode = CodeGenerator.GenerateUntypedTree(domainModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("UntypedTree.Generated.cs", untypedTreeCode),
        };
    }
}
