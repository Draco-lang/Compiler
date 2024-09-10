using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Draco.SourceGeneration.SyntaxTree;

[Generator]
public sealed class SyntaxTreeSourceGenerator : XmlSourceGenerator
{
    protected override string XmlFileName => "Syntax.xml";
    protected override Type XmlModelType => typeof(XmlTree);

    protected override IEnumerable<KeyValuePair<string, string>> GenerateSources(object xmlModel, CancellationToken cancellationToken)
    {
        var domainModel = Tree.FromXml((XmlTree)xmlModel);

        var tokenCode = Template.GenerateTokens(domainModel);
        var greenTreeCode = Template.GenerateGreenTree(domainModel);
        var redTreeCode = Template.GenerateRedTree(domainModel);
        var quoterCode = Template.GenerateQuoter(domainModel);

        return
        [
            new("Tokens.Generated.cs", tokenCode),
            new("GreenSyntaxTree.Generated.cs", greenTreeCode),
            new("RedSyntaxTree.Generated.cs", redTreeCode),
            new("Quoter.Generated.cs", quoterCode),
        ];
    }
}
