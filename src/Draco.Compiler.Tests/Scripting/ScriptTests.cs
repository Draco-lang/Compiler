using Draco.Compiler.Api.Scripting;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Scripting;

public sealed class ScriptTests
{
    [Fact]
    public void BasicAssignmentAndAddition()
    {
        // Arrange
        var script = Script.Create<int>("""
            var x = 3;
            var y = 4;
            x + y
            """,
            metadataReferences: BclReferences);

        // Act
        var result = script.Execute();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(7, result.Value);
    }

    [Fact]
    public void SyntaxErrorInScript()
    {
        // Arrange
        var script = Script.Create<int>("""
            var x = ;
            """,
            metadataReferences: BclReferences);

        // Act
        var result = script.Execute();

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Diagnostics);
    }
}
