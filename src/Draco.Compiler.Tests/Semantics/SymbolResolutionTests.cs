using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Semantics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using IInternalScope = Draco.Compiler.Internal.Semantics.Symbols.IScope;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests
{
    private static TSymbol GetInternalSymbol<TSymbol>(ISymbol? symbol)
        where TSymbol : IInternalSymbol
    {
        Assert.NotNull(symbol);
        var symbolBase = (SymbolBase)symbol!;
        return (TSymbol)symbolBase.Symbol;
    }

    private static void AssertParentOf(IInternalScope? parent, IInternalScope? child)
    {
        Assert.NotNull(child);
        Assert.False(ReferenceEquals(parent, child));
        Assert.True(ReferenceEquals(child!.Parent, parent));
    }

    [Fact]
    public void BasicScopeTree()
    {
        // func foo(n: int32) { // b1
        //     var x1;
        //     {                // b2
        //         var x2;
        //         { var x3; }  // b3
        //     }
        //     {                // b4
        //         var x4;
        //         { var x5; }  // b5
        //         { var x6; }  // b6
        //     }
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            ImmutableArray.Create(
                FuncParam(Name("n"), NameTypeExpr(Name("int32")))),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x1"))),
                ExprStmt(BlockExpr(
                    DeclStmt(VariableDecl(Name("x2"))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x3"))))))),
                ExprStmt(BlockExpr(
                    DeclStmt(VariableDecl(Name("x4"))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x5"))))),
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x6")))))))))));

        var foo = tree.FindInChildren<ParseTree.Decl.Func>();
        var n = tree.FindInChildren<ParseTree.FuncParam>();
        var x1 = tree.FindInChildren<ParseTree.Decl.Variable>(0);
        var x2 = tree.FindInChildren<ParseTree.Decl.Variable>(1);
        var x3 = tree.FindInChildren<ParseTree.Decl.Variable>(2);
        var x4 = tree.FindInChildren<ParseTree.Decl.Variable>(3);
        var x5 = tree.FindInChildren<ParseTree.Decl.Variable>(4);
        var x6 = tree.FindInChildren<ParseTree.Decl.Variable>(5);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symFoo = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(foo));
        var symn = GetInternalSymbol<IInternalSymbol.IParameter>(semanticModel.GetDefinedSymbolOrNull(n));
        var sym1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x1));
        var sym2 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x2));
        var sym3 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x3));
        var sym4 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x4));
        var sym5 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x5));
        var sym6 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x6));

        // Assert
        AssertParentOf(sym2.DefiningScope, sym3.DefiningScope);
        AssertParentOf(sym1.DefiningScope, sym2.DefiningScope);
        AssertParentOf(sym4.DefiningScope, sym5.DefiningScope);
        AssertParentOf(sym4.DefiningScope, sym6.DefiningScope);
        AssertParentOf(sym1.DefiningScope, sym4.DefiningScope);

        AssertParentOf(symn.DefiningScope, sym1.DefiningScope);

        AssertParentOf(symFoo.DefiningScope, symn.DefiningScope);
        Assert.True(ReferenceEquals(symFoo.DefinedScope, symn.DefiningScope));
    }

    [Fact]
    public void LocalShadowing()
    {
        // func foo() {
        //     var x = 0;
        //     var x = x + 1;
        //     var x = x + 1;
        //     var x = x + 1;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            ImmutableArray<ParseTree.FuncParam>.Empty,
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), null, LiteralExpr(0))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1)))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1)))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1))))))));

        var x0 = tree.FindInChildren<ParseTree.Decl.Variable>(0);
        var x1 = tree.FindInChildren<ParseTree.Decl.Variable>(1);
        var x2 = tree.FindInChildren<ParseTree.Decl.Variable>(2);
        var x3 = tree.FindInChildren<ParseTree.Decl.Variable>(3);

        var x0ref = tree.FindInChildren<ParseTree.Expr.Name>(0);
        var x1ref = tree.FindInChildren<ParseTree.Expr.Name>(1);
        var x2ref = tree.FindInChildren<ParseTree.Expr.Name>(2);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symx0 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x0));
        var symx1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x1));
        var symx2 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x2));
        var symx3 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x3));

        var symRefx0 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbolOrNull(x0ref));
        var symRefx1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbolOrNull(x1ref));
        var symRefx2 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbolOrNull(x2ref));

        // Assert
        Assert.False(ReferenceEquals(symx0, symx1));
        Assert.False(ReferenceEquals(symx1, symx2));
        Assert.False(ReferenceEquals(symx2, symx3));

        Assert.True(ReferenceEquals(symx0, symRefx0));
        Assert.True(ReferenceEquals(symx1, symRefx1));
        Assert.True(ReferenceEquals(symx2, symRefx2));
    }

    [Fact]
    public void OrderIndependentReferencing()
    {
        // func bar() = foo();
        // func foo() = foo();
        // func baz() = foo();

        // Arrange
        var tree = CompilationUnit(
            FuncDecl(
                Name("bar"),
                ImmutableArray<ParseTree.FuncParam>.Empty,
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))),
            FuncDecl(
                Name("foo"),
                ImmutableArray<ParseTree.FuncParam>.Empty,
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))),
            FuncDecl(
                Name("baz"),
                ImmutableArray<ParseTree.FuncParam>.Empty,
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))));

        var barDecl = tree.FindInChildren<ParseTree.Decl.Func>(0);
        var fooDecl = tree.FindInChildren<ParseTree.Decl.Func>(1);
        var bazDecl = tree.FindInChildren<ParseTree.Decl.Func>(2);

        var call1 = tree.FindInChildren<ParseTree.Expr.Call>(0);
        var call2 = tree.FindInChildren<ParseTree.Expr.Call>(1);
        var call3 = tree.FindInChildren<ParseTree.Expr.Call>(2);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symBar = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(barDecl));
        var symFoo = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(fooDecl));
        var symBaz = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(bazDecl));

        var refFoo1 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call1.Called));
        var refFoo2 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call2.Called));
        var refFoo3 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call3.Called));

        // Assert
        Assert.False(ReferenceEquals(symBar, symFoo));
        Assert.False(ReferenceEquals(symFoo, symBaz));

        Assert.True(ReferenceEquals(symFoo, refFoo1));
        Assert.True(ReferenceEquals(symFoo, refFoo2));
        Assert.True(ReferenceEquals(symFoo, refFoo3));
    }

    [Fact]
    public void OrderDependentReferencing()
    {
        // func foo() {
        //     var x;
        //     var y = x + z;
        //     var z;
        // }

        // Arrange
        var tree = CompilationUnit(FuncDecl(
            Name("foo"),
            ImmutableArray<ParseTree.FuncParam>.Empty,
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"))),
                DeclStmt(VariableDecl(Name("y"), value: BinaryExpr(NameExpr("x"), Plus, NameExpr("z")))),
                DeclStmt(VariableDecl(Name("z")))))));

        var xDecl = tree.FindInChildren<ParseTree.Decl.Variable>(0);
        var yDecl = tree.FindInChildren<ParseTree.Decl.Variable>(1);
        var zDecl = tree.FindInChildren<ParseTree.Decl.Variable>(2);

        var xRef = tree.FindInChildren<ParseTree.Expr.Name>(0);
        var zRef = tree.FindInChildren<ParseTree.Expr.Name>(1);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symx = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));
        var symy = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(yDecl));
        var symz = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(zDecl));

        var symRefx = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbol(xRef));
        var symRefz = GetInternalSymbol<IInternalSymbol>(semanticModel.GetReferencedSymbol(zRef));

        // Assert
        Assert.True(ReferenceEquals(symx, symRefx));
        Assert.False(ReferenceEquals(symz, symRefz));
        Assert.True(symRefz.IsError);
    }
}
