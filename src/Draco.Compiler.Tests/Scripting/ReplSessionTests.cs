using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;
using static Basic.Reference.Assemblies.Net80;

namespace Draco.Compiler.Tests.Scripting;

public sealed class ReplSessionTests
{
    private static IEnumerable<MetadataReference> BclReferences => ReferenceInfos.All
        .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)));

    [InlineData("", null)]
    [InlineData("// hello", null)]
    [InlineData("1 + 2", 3)]
    [InlineData("2 < 3 < 4", true)]
    [InlineData("\"asd\" + \"def\"", "asddef")]
    [InlineData("\"1 + 2 = \\{1 + 2}\"", "1 + 2 = 3")]
    [InlineData("func foo() {}", null)]
    [Theory]
    public void BasicExpressions(string input, object? output)
    {
        var replSession = new ReplSession([.. BclReferences]);

        var result = replSession.Evaluate(input);

        Assert.True(result.Success);
        Assert.Equal(output, result.Value);
    }

    [InlineData("func add(x: int32, y: int32) = x + y;")]
    [Theory]
    public void InvalidEntries(string input)
    {
        var replSession = new ReplSession([.. BclReferences]);

        var result = replSession.Evaluate(input);

        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
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

    [Fact]
    public void ComplexSession3() => AssertSequence(
        ("func id<T>(x: T): T = x;", null),
        ("id(1)", 1),
        ("id(\"asd\")", "asd"));

    [Fact]
    public void ComplexSession4() => AssertSequence(
        ("var x = 4;", null),
        ("func foo(): int32 = x;", null),
        ("foo()", 4),
        ("var x = 5;", null),
        ("foo()", 4),
        ("func foo(): int32 = x;", null),
        ("foo()", 5));

    [Fact]
    public void ComplexSession5() => AssertSequence(
        ("import System.Collections.Generic;", null),
        ("var l = List<int32>();", null),
        ("l.Add(1);", null),
        ("l.Add(2);", null),
        ("l.Add(3);", null),
        ("l.Count", 3));

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
