using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.FlowAnalysis.Domains;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class ReturnsOnAllPathsTests
{
    [Fact]
    public void UnitMethodDoesNotReturn()
    {
        // Arrange
        // func foo() {}
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration("foo", ParameterList(), null, BlockFunctionBody())));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonUnitMethodDoesNotReturn()
    {
        // Arrange
        // func foo(): int32 {}
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration("foo", ParameterList(), NameType("int32"), BlockFunctionBody())));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void NonUnitMethodWithNoExitPointDoesNotReturn()
    {
        // Arrange
        // func foo(): int32 {
        // start:
        //     goto start;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                DeclarationStatement(LabelDeclaration("start")),
                ExpressionStatement(GotoExpression(NameLabel("start")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }
}
