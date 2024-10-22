using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class DefiniteAssignmentTests
{
    [Fact]
    public void UseOfUnassignedVariable()
    {
        // Arrange
        // func main() {
        //     var x: int32;
        //     var y = x;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                DeclarationStatement(VariableDeclaration("y", NameType("int32"), NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void UseOfAssignedVariable()
    {
        // Arrange
        // func main() {
        //     var x: int32 = 0;
        //     var y = x;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"), LiteralExpression(0))),
                DeclarationStatement(VariableDeclaration("y", NameType("int32"), NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void UseOfConditionallyAssignedVariable()
    {
        // Arrange
        // func foo(b: bool) {
        //     var x: int32;
        //     if (b) x = 42;
        //     var y = x;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    BinaryExpression(NameExpression("x"), Assign, LiteralExpression(42)),
                    null as ExpressionSyntax)),
                DeclarationStatement(VariableDeclaration("y", NameType("int32"), NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void UseOfConditionallyAssignedVariableOnBothBranches()
    {
        // Arrange
        // func foo(b: bool) {
        //     var x: int32;
        //     if (b) x = 42; else x = 0;
        //     var y = x;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    NameExpression("b"),
                    BinaryExpression(NameExpression("x"), Assign, LiteralExpression(42)),
                    BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)))),
                DeclarationStatement(VariableDeclaration("y", NameType("int32"), NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void JumpingMidAssignment()
    {
        // Arrange
        // func foo(b: bool) {
        //     var x: int32 = if (b) goto end else 42;
        // end:
        //     var y = x;
        // }
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"), IfExpression(
                    NameExpression("b"),
                    GotoExpression(NameLabel("end")),
                    LiteralExpression(42)))),
                DeclarationStatement(LabelDeclaration("end")),
                DeclarationStatement(VariableDeclaration("y", NameType("int32"), NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostics(diagnostics, FlowAnalysisErrors.VariableUsedBeforeInit);
    }
}
