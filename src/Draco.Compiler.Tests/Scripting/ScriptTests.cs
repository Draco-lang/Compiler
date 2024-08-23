using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Scripting;

namespace Draco.Compiler.Tests.Scripting;

public sealed class ScriptTests
{
    [Fact]
    public void BasicAssignmentAndAddition()
    {
        // Arrange
        var script = ScriptingEngine.CreateScript<int>("""
            var x = 3;
            var y = 4;
            x + y
            """,
            // TODO: We could factor out BCL refs into some global, we repeat this LINQ a lot in tests
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());

        // Act
        var result = script.Execute();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(7, result.Value);
    }
}
