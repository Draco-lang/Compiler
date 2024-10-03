using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.FlowAnalysis;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class FlowAnalysisTests
{
    [Fact]
    public void UnitFunctionReturnsImplicitly()
    {
        // func foo() {
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody())));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void NonUnitFunctionDoesNotReturnImplicitly()
    {
        // func foo(): int32 {
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody())));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void NonUnitFunctionReturnsExplicitly()
    {
        // func foo(): int32 {
        //     return 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(ReturnExpression(LiteralExpression(0)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReturnInOnlyOneBranchOfIf()
    {
        // func foo(b: bool): int32 {
        //     if (b) return 1;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(IfExpression(
                    condition: NameExpression("b"),
                    then: ReturnExpression(LiteralExpression(1))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void ReturnInBothBranchesOfIf()
    {
        // func foo(b: bool): int32 {
        //     if (b) return 1;
        //     else return 2;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("b", NameType("bool"))),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(IfExpression(
                    condition: NameExpression("b"),
                    then: ReturnExpression(LiteralExpression(1)),
                    @else: ReturnExpression(LiteralExpression(2))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReturnInWhileBody()
    {
        // func foo(): int32 {
        //     while (false) {
        //         return 0;
        //     }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(
                    condition: LiteralExpression(false),
                    then: ReturnExpression(LiteralExpression(0))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void GotoOverReturn()
    {
        // func foo(): int32 {
        //     goto after;
        //     return 0;
        // after:
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(GotoExpression("after")),
                ExpressionStatement(ReturnExpression(LiteralExpression(0))),
                DeclarationStatement(LabelDeclaration("after"))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void GotoOverReturnThenJumpBack()
    {
        // func foo(): int32 {
        //     goto after;
        // before:
        //     return 0;
        // after:
        //     goto before;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(GotoExpression("after")),
                DeclarationStatement(LabelDeclaration("before")),
                ExpressionStatement(ReturnExpression(LiteralExpression(0))),
                DeclarationStatement(LabelDeclaration("after")),
                ExpressionStatement(GotoExpression("before"))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void GotoToNonExistingLabelDoesNotCascadeDataflowErrors()
    {
        // func foo(): int32 {
        //     goto non_existing;
        //     return 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(GotoExpression("non_existing")),
                ExpressionStatement(ReturnExpression(LiteralExpression(0)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertNotDiagnostics(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void ReferenceUninitializedVariable()
    {
        // func foo() {
        //     var x: int32;
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void ReferenceInitializedVariable()
    {
        // func foo() {
        //     var x: int32 = 0;
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReferenceInitializedVariableAssignedLater()
    {
        // func foo() {
        //     var x: int32;
        //     x = 0;
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReferenceVariableInitializedOnlyInOneBranch()
    {
        // func foo() {
        //     var x: int32;
        //     if (false) {
        //         x = 0;
        //     }
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    condition: LiteralExpression(false),
                    then: BlockExpression(ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)))))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void ReferenceVariableInitializedInBothBranches()
    {
        // func foo() {
        //     var x: int32;
        //     if (false) {
        //         x = 0;
        //     }
        //     else {
        //         x = 1;
        //     }
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(IfExpression(
                    condition: LiteralExpression(false),
                    then: BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)),
                    @else: BinaryExpression(NameExpression("x"), Assign, LiteralExpression(1)))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReferenceVariableInitializedOnlyInLoopBody()
    {
        // func foo() {
        //     var x: int32;
        //     while (false) {
        //         x = 0;
        //     }
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(true, "x", NameType("int32"))),
                ExpressionStatement(WhileExpression(
                    condition: LiteralExpression(false),
                    then: BlockExpression(
                        ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)))))),
                DeclarationStatement(VariableDeclaration(true, "y", null, NameExpression("x")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void GobalValInitialized()
    {
        // val x: int32 = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImmutableVariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void LocalValInitialized()
    {
        // func foo() {
        //     val x: int32 = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32"), LiteralExpression(0)))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void GobalValNotInitialized()
    {
        // val x: int32;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImmutableVariableDeclaration(true, "x", NameType("int32"))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.ImmutableVariableMustBeInitialized);
    }

    [Fact]
    public void LocalValNotInitialized()
    {
        // func foo() {
        //     val x: int32;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImmutableVariableDeclaration(true, "x", NameType("int32")))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.ImmutableVariableMustBeInitialized);
    }

    [Fact]
    public void LocalValReassigned()
    {
        // func foo() {
        //     val x: int32 = 0;
        //     x = 1;
        // }

        // Arrange
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
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo);
    }

    [Fact]
    public void GlobalValReassigned()
    {
        // val x: int32 = 0;
        // func foo() {    
        //     x = 1;
        // }

        // Arrange
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
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostics(diags, FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo);
    }
}
