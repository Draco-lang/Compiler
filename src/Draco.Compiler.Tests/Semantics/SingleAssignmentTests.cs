using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SingleAssignmentTests
{
    [Fact]
    public void AssignImmutableOnce()
    {
        // Arrange
        // func foo() {
        //     val x: int32 = 0;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void AssignImmutableTwice()
    {
        // Arrange
        // func foo() {
        //     val x: int32 = 0;
        //     x = 1;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0))),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.ImmutableVariableAssignedMultipleTimes);
    }

    [Fact]
    public void AssignImmutableInMutuallyExclusiveBranches()
    {
        // Arrange
        // func foo(b: bool) {
        //     val x: int32;
        //     if (b) x = 1; else x = 2;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    StatementExpression(ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1)))),
                    StatementExpression(ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(2))))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void ConditionallyAndThenUnconditionallyAssignImmutable()
    {
        // Arrange
        // func foo(b: bool) {
        //     val x: int32;
        //     if (b) x = 1;
        //     x = 2;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    StatementExpression(ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1)))))),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(2)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.ImmutableVariableAssignedMultipleTimes);
    }

    [Fact]
    public void AssignImmutableInLoop()
    {
        // Arrange
        // func foo(b: bool) {
        //     val x: int32;
        //     while (b) x = 1;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(WhileExpression(
                    NameExpression("b"),
                    StatementExpression(ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1))))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.ImmutableVariableAssignedMultipleTimes);
    }

    [Fact]
    public void GlobalImmutableReassigned()
    {
        // Arrange
        // val x: int32 = 0;
        // func foo() {
        //     x = 1;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(
            ImmutableVariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0)),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.ImmutableVariableAssignedMultipleTimes);
    }
}
