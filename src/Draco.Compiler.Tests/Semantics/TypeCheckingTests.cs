using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
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
            ImmutableArray<ParseTree.FuncParam>.Empty,
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
            ImmutableArray<ParseTree.FuncParam>.Empty,
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
            ImmutableArray<ParseTree.FuncParam>.Empty,
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
            ImmutableArray<ParseTree.FuncParam>.Empty,
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
}
