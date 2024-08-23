using Draco.Compiler.Api.Scripting;

namespace Draco.Compiler.Tests.Scripting;

public sealed class ScriptTests
{
    public void BasicAssignmentAndAddition()
    {
        // Arrange
        var script = ScriptingEngine.CreateScript<int>("""
            var x = 3;
            var y = 4;
            x + y
            """);

        // Act
        var result = script.Execute();

        // Assert
        Assert.True(result.Success);
        Assert.Equal(7, result.Value);
    }
}
