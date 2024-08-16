using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using Draco.Compiler.Internal.Utilities;
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

    [Fact]
    public void ComplexSession1() => AssertSequence(
        ("var x = 3;", null),
        ("var y = 4;", null),
        ("var z = x + y;", null),
        ("z", 7),
        ("var x = 5;", null),
        ("x", 5),
        ("z", 7));

    [Fact]
    public void ComplexSession2() => AssertSequence(
        ("func add(x: int32, y: int32): int32 = x + y;", null),
        ("add(1, 2);", null),
        ("add(1, 2)", 3));

    private static void AssertSequence(params (string Code, object? Value)[] pairs)
    {
        var results = ExecuteSequence(pairs.Select(p => p.Code));
        foreach (var (expected, result) in pairs.Select(p => p.Value).Zip(results))
        {
            Assert.True(result.Success);
            Assert.Equal(expected, result.Value);
        }
    }

    private static IEnumerable<ExecutionResult<object?>> ExecuteSequence(IEnumerable<string> inputs)
    {
        var replSession = new ReplSession([.. BclReferences]);

        var ms = new MemoryStream();
        var reader = new StreamReader(ms);
        var writer = new StreamWriter(ms);

        foreach (var input in inputs)
        {
            var pos = ms.Position;

            writer.WriteLine(input);
            writer.Flush();

            ms.Position = pos;

            yield return replSession.Evaluate(reader);
        }
    }
}
