using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class DataFlowAnalysisTests : SemanticTestsBase
{
    [Fact]
    public void UnitFunctionReturnsImplicitly()
    {
        // func foo() {
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr()))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr()))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void NonUnitFunctionReturnsExplicitly()
    {
        // func foo(): int32 {
        //     return 0;
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(ReturnExpr(LiteralExpr(0))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("bool")))),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(IfExpr(
                    condition: NameExpr("b"),
                    then: ReturnExpr(LiteralExpr(1)))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReturnInBothBranchesOfIf()
    {
        // func foo(b: bool): int32 {
        //     if (b) return 1;
        //     else return 2;
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("bool")))),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(IfExpr(
                    condition: NameExpr("b"),
                    then: ReturnExpr(LiteralExpr(1)),
                    @else: ReturnExpr(LiteralExpr(2)))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("bool")))),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(WhileExpr(
                    condition: LiteralExpr(false),
                    body: ReturnExpr(LiteralExpr(0)))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("bool")))),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(GotoExpr("after")),
                ExprStmt(ReturnExpr(LiteralExpr(0))),
                DeclStmt(LabelDecl("after")))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("bool")))),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(GotoExpr("after")),
                DeclStmt(LabelDecl("before")),
                ExprStmt(ReturnExpr(LiteralExpr(0))),
                DeclStmt(LabelDecl("after")),
                ExprStmt(GotoExpr("before")))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void ReferenceUninitializedVariable()
    {
        // func foo() {
        //     var x: int32;
        //     var y = x;
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void ReferenceInitializedVariable()
    {
        // func foo() {
        //     var x: int32 = 0;
        //     var y = x;
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")), LiteralExpr(0))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")))),
                ExprStmt(BinaryExpr(NameExpr("x"), Assign, LiteralExpr(0))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")))),
                ExprStmt(IfExpr(
                    condition: LiteralExpr(false),
                    then: BinaryExpr(NameExpr("x"), Assign, LiteralExpr(0)))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")))),
                ExprStmt(IfExpr(
                    condition: LiteralExpr(false),
                    then: BinaryExpr(NameExpr("x"), Assign, LiteralExpr(0)),
                    @else: BinaryExpr(NameExpr("x"), Assign, LiteralExpr(1)))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")))),
                ExprStmt(WhileExpr(
                    condition: LiteralExpr(false),
                    body: BinaryExpr(NameExpr("x"), Assign, LiteralExpr(0)))),
                DeclStmt(VariableDecl(Name("y"), null, NameExpr("x"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }
}
