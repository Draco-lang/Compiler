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
}
