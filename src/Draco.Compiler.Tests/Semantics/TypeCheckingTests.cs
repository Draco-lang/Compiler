using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Tests.Semantics;

public sealed class TypeCheckingTests : SemanticTestsBase
{
    [Fact]
    public void VariableExplicitlyTyped()
    {
        // func main() {
        //     var x: int32 = 0;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32")), LiteralExpr(0)))))));

        var xDecl = tree.FindInChildren<ParseTree.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.GetAllDiagnostics());
        Assert.Equal(xSym.Type, Type.Int32);
    }

    [Fact]
    public void VariableTypeInferredFromValue()
    {
        // func main() {
        //     var x = 0;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), value: LiteralExpr(0)))))));

        var xDecl = tree.FindInChildren<ParseTree.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.GetAllDiagnostics());
        Assert.Equal(xSym.Type, Type.Int32);
    }

    [Fact]
    public void VariableExplicitlyTypedWithoutValue()
    {
        // func main() {
        //     var x: int32;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), NameTypeExpr(Name("int32"))))))));

        var xDecl = tree.FindInChildren<ParseTree.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.GetAllDiagnostics());
        Assert.Equal(xSym.Type, Type.Int32);
    }

    [Fact]
    public void VariableTypeInferredFromLaterAssignment()
    {
        // func main() {
        //     var x;
        //     x = 0;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"))),
                ExprStmt(BinaryExpr(NameExpr("x"), Assign, LiteralExpr(0)))))));

        var xDecl = tree.FindInChildren<ParseTree.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.GetAllDiagnostics());
        Assert.Equal(xSym.Type, Type.Int32);
    }

    [Fact]
    public void BlockBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 {
        //     return "Hello";
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(ReturnExpr(StringExpr("Hello")))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void InlineBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 = "Hello";

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            InlineBodyFuncBody(StringExpr("Hello"))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void IfConditionIsBool()
    {
        // func foo() {
        //     if (true) {}
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(IfExpr(LiteralExpr(true), BlockExpr()))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void IfConditionIsNotBool()
    {
        // func foo() {
        //     if (1) {}
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(IfExpr(LiteralExpr(1), BlockExpr()))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void WhileConditionIsBool()
    {
        // func foo() {
        //     while (true) {}
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(WhileExpr(LiteralExpr(true), BlockExpr()))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Empty(diags);
    }

    [Fact]
    public void WhileConditionIsNotBool()
    {
        // func foo() {
        //     while (1) {}
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                ExprStmt(WhileExpr(LiteralExpr(1), BlockExpr()))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void IfElseTypeMismatch()
    {
        // func foo() {
        //     var x = if (true) 0 else "Hello";
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            NameTypeExpr(Name("int32")),
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(
                    Name("x"),
                    value: IfExpr(
                        condition: LiteralExpr(true),
                        then: LiteralExpr(0),
                        @else: StringExpr("Hello"))))))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();
        var diags = semanticModel.GetAllDiagnostics();

        // Assert
        Assert.Single(diags);
        Assert.True(diags.First().Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    public void AllowNonUnitExpressionInInlineFuncReturningUnit()
    {
        // func foo(): int32 = 0;
        // func bar() = foo();

        // Arrange
        var tree = CompilationUnit(
            FuncDecl(
                Name("foo"),
                FuncParamList(),
                NameTypeExpr(Name("int32")),
                InlineBodyFuncBody(LiteralExpr(0))),
            FuncDecl(
                Name("bar"),
                FuncParamList(),
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))));

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var diags = semanticModel.GetAllDiagnostics();
        var fooDecl = tree.FindInChildren<ParseTree.Decl.Func>(0);
        var barDecl = tree.FindInChildren<ParseTree.Decl.Func>(1);
        var fooSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(fooDecl));
        var barSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(barDecl));

        // Assert
        Assert.Empty(diags);
        Assert.True(ReferenceEquals(fooSymbol.ReturnType, Type.Int32));
        Assert.True(ReferenceEquals(barSymbol.ReturnType, Type.Unit));
    }
}
