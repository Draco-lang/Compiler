using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Tests.Repl;

public sealed class BasicSessionTest
{
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    [InlineData("1 + 2", 3)]
    [InlineData("2 < 3 < 4", true)]
    [InlineData("\"asd\" + \"def\"", "asddef")]
    [InlineData("\"1 + 2 = \\{1 + 2}\"", "1 + 2 = 3")]
    [Theory]
    public void BasicExpressions(string input, object? output)
    {
        var replSession = new ReplSession([.. BclReferences]);

        var ms = new MemoryStream();

        var writer = new StreamWriter(ms);
        writer.WriteLine(input);
        writer.Flush();

        ms.Position = 0;
        var reader = new StreamReader(ms);

        var result = replSession.Evaluate(reader);

        Assert.True(result.Success);
        Assert.Equal(output, result.Value);
    }
}
