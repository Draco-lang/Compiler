using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Draco.SourceGeneration.SyntaxTree;

[Generator]
public sealed class SyntaxTreeSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "Syntax.xml";
    protected override Type XmlModelType => typeof(XmlTree);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Tree.FromXml((XmlTree)xmlModel);

        var greenTreeCode = CodeGenerator.GenerateGreenSyntaxTree(domainModel, cancellationToken);
        var redTreeCode = CodeGenerator.GenerateRedSyntaxTree(domainModel, cancellationToken);

        return new KeyValuePair<string, string>[]
        {
            new("GreenSyntaxTree.Generated.cs", greenTreeCode),
            new("RedSyntaxTree.Generated.cs", redTreeCode),
        };
    }
}
