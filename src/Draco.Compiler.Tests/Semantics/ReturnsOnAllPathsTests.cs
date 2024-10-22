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

    [Fact]
    public void NonUnitMethodReturnsUnconditionally()
    {
        // Arrange
        // func foo(): int32 {
        //     return 42;
        //
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(ExpressionStatement(ReturnExpression(LiteralExpression(42)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonUnitMethodReturnsConditionally()
    {
        // Arrange
        // func foo(b: bool): int32 {
        //     if (b) return 42;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    ReturnExpression(LiteralExpression(42))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void NonUnitMethodReturnsConditionallyButThenUnconditionally()
    {
        // Arrange
        // func foo(b: bool): int32 {
        //     if (b) return 42;
        //     return 0;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    ReturnExpression(LiteralExpression(42)))),
                ExpressionStatement(ReturnExpression(LiteralExpression(0)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void NonUnitMethodReturnsInConditionalLoop()
    {
        // Arrange
        // func foo(b: bool): int32 {
        //     while (b) return 42;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(
                    NameExpression("b"),
                    ReturnExpression(LiteralExpression(42))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.DoesNotReturn);
    }
}
