using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using IInternalScope = Draco.Compiler.Internal.Semantics.Symbols.IScope;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests : SemanticTestsBase
{
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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(
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
                    ExprStmt(BlockExpr(DeclStmt(VariableDecl(Name("x6"))))))))))));

        var foo = tree.FindInChildren<ParseNode.Decl.Func>();
        var n = tree.FindInChildren<ParseNode.FuncParam>();
        var x1 = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var x2 = tree.FindInChildren<ParseNode.Decl.Variable>(1);
        var x3 = tree.FindInChildren<ParseNode.Decl.Variable>(2);
        var x4 = tree.FindInChildren<ParseNode.Decl.Variable>(3);
        var x5 = tree.FindInChildren<ParseNode.Decl.Variable>(4);
        var x6 = tree.FindInChildren<ParseNode.Decl.Variable>(5);

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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"), null, LiteralExpr(0))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1)))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1)))),
                DeclStmt(VariableDecl(Name("x"), null, BinaryExpr(NameExpr("x"), Plus, LiteralExpr(1)))))))));

        var x0 = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var x1 = tree.FindInChildren<ParseNode.Decl.Variable>(1);
        var x2 = tree.FindInChildren<ParseNode.Decl.Variable>(2);
        var x3 = tree.FindInChildren<ParseNode.Decl.Variable>(3);

        var x0ref = tree.FindInChildren<ParseNode.Expr.Name>(0);
        var x1ref = tree.FindInChildren<ParseNode.Expr.Name>(1);
        var x2ref = tree.FindInChildren<ParseNode.Expr.Name>(2);

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
        var tree = ParseTree.Create(CompilationUnit(
            FuncDecl(
                Name("bar"),
                FuncParamList(),
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))),
            FuncDecl(
                Name("foo"),
                FuncParamList(),
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo")))),
            FuncDecl(
                Name("baz"),
                FuncParamList(),
                null,
                InlineBodyFuncBody(CallExpr(NameExpr("foo"))))));

        var barDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);
        var fooDecl = tree.FindInChildren<ParseNode.Decl.Func>(1);
        var bazDecl = tree.FindInChildren<ParseNode.Decl.Func>(2);

        var call1 = tree.FindInChildren<ParseNode.Expr.Call>(0);
        var call2 = tree.FindInChildren<ParseNode.Expr.Call>(1);
        var call3 = tree.FindInChildren<ParseNode.Expr.Call>(2);

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
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"))),
                DeclStmt(VariableDecl(Name("y"), value: BinaryExpr(NameExpr("x"), Plus, NameExpr("z")))),
                DeclStmt(VariableDecl(Name("z"))))))));

        var xDecl = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var yDecl = tree.FindInChildren<ParseNode.Decl.Variable>(1);
        var zDecl = tree.FindInChildren<ParseNode.Decl.Variable>(2);

        var xRef = tree.FindInChildren<ParseNode.Expr.Name>(0);
        var zRef = tree.FindInChildren<ParseNode.Expr.Name>(1);

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

    [Fact]
    public void OrderDependentReferencingWithNesting()
    {
        // func foo() {
        //     var x;                 // x1
        //     {
        //         var y;             // y1
        //         var z = x + y;     // z1, x1, y1
        //         var x;             // x2
        //         {
        //             var k = x + w; // k1, x2, error
        //         }
        //         var w;             // w1
        //     }
        //     var k = w;             // k2, error
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
                DeclStmt(VariableDecl(Name("x"))),
                ExprStmt(BlockExpr(
                    DeclStmt(VariableDecl(Name("y"))),
                    DeclStmt(VariableDecl(Name("z"), value: BinaryExpr(NameExpr("x"), Plus, NameExpr("y")))),
                    DeclStmt(VariableDecl(Name("x"))),
                    ExprStmt(BlockExpr(
                        DeclStmt(VariableDecl(Name("k"), value: BinaryExpr(NameExpr("x"), Plus, NameExpr("w")))))),
                    DeclStmt(VariableDecl(Name("w"))))),
                DeclStmt(VariableDecl(Name("k"), value: NameExpr("w"))))))));

        var x1Decl = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var y1Decl = tree.FindInChildren<ParseNode.Decl.Variable>(1);
        var z1Decl = tree.FindInChildren<ParseNode.Decl.Variable>(2);
        var x2Decl = tree.FindInChildren<ParseNode.Decl.Variable>(3);
        var k1Decl = tree.FindInChildren<ParseNode.Decl.Variable>(4);
        var w1Decl = tree.FindInChildren<ParseNode.Decl.Variable>(5);
        var k2Decl = tree.FindInChildren<ParseNode.Decl.Variable>(6);

        var x1Ref1 = tree.FindInChildren<ParseNode.Expr.Name>(0);
        var y1Ref1 = tree.FindInChildren<ParseNode.Expr.Name>(1);
        var x2Ref1 = tree.FindInChildren<ParseNode.Expr.Name>(2);
        var wRefErr1 = tree.FindInChildren<ParseNode.Expr.Name>(3);
        var wRefErr2 = tree.FindInChildren<ParseNode.Expr.Name>(4);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var x1SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x1Decl));
        var y1SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(y1Decl));
        var z1SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(z1Decl));
        var x2SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(x2Decl));
        var k1SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(k1Decl));
        var w1SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(w1Decl));
        var k2SymDecl = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(k2Decl));

        var x1SymRef1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbol(x1Ref1));
        var y1SymRef1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbol(y1Ref1));
        var x2SymRef1 = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbol(x2Ref1));
        var wSymRef1 = semanticModel.GetReferencedSymbol(wRefErr1);
        var wSymRef2 = semanticModel.GetReferencedSymbol(wRefErr2);

        // Assert
        Assert.True(ReferenceEquals(x1SymDecl, x1SymRef1));
        Assert.True(ReferenceEquals(y1SymDecl, y1SymRef1));
        Assert.True(ReferenceEquals(x2SymDecl, x2SymRef1));
        // TODO: Maybe we should still resolve the reference, but mark it that it's something that comes later?
        // (so it is still an error)
        // It would definitely help reduce error cascading
        Assert.True(wSymRef1.IsError);
        Assert.True(wSymRef2.IsError);
    }

    [Fact]
    public void ParameterRedefinitionError()
    {
        // func foo(x: int32, x: int32) {
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(FuncDecl(
            Name("foo"),
            FuncParamList(
                FuncParam(Name("x"), NameTypeExpr(Name("int32"))),
                FuncParam(Name("x"), NameTypeExpr(Name("int32")))),
            null,
            BlockBodyFuncBody(BlockExpr()))));

        var x1Decl = tree.FindInChildren<ParseNode.FuncParam>(0);
        var x2Decl = tree.FindInChildren<ParseNode.FuncParam>(1);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var x1SymDecl = GetInternalSymbol<IInternalSymbol.IParameter>(semanticModel.GetDefinedSymbolOrNull(x1Decl));
        var x2SymDecl = semanticModel.GetDefinedSymbolOrNull(x2Decl);

        // Assert
        Assert.False(x1SymDecl.IsError);
        Assert.NotNull(x2SymDecl);
        Assert.True(x2SymDecl!.IsError);
    }

    [Fact]
    public void FuncOverloadsGlobalVar()
    {
        // var b: int32;
        // func b(b: int32): int32 = b;

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(
            VariableDecl(Name("b"), NameTypeExpr(Name("int32"))),
            FuncDecl(
                Name("b"),
                FuncParamList(FuncParam(Name("b"), NameTypeExpr(Name("int32")))),
                NameTypeExpr(Name("int32")),
                InlineBodyFuncBody(NameExpr("b")))));

        var varDecl = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var funcDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var varSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(varDecl));
        var funcSym = semanticModel.GetDefinedSymbolOrNull(funcDecl);

        // Assert
        Assert.False(varSym.IsError);
        Assert.NotNull(funcSym);
        Assert.True(funcSym!.IsError);
    }

    [Fact]
    public void GlobalVariableDefinedLater()
    {
        // func foo() {
        //     var y = x;
        // }
        // var x;

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(
            FuncDecl(
                Name("foo"),
                FuncParamList(),
                null,
                BlockBodyFuncBody(BlockExpr(
                    DeclStmt(VariableDecl(Name("y"), value: NameExpr("x")))))),
            VariableDecl(Name("x"))));

        var localVarDecl = tree.FindInChildren<ParseNode.Decl.Variable>(0);
        var globalVarDecl = tree.FindInChildren<ParseNode.Decl.Variable>(1);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var varRefSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbolOrNull(localVarDecl.Initializer!.Value));
        var varDeclSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(globalVarDecl));

        // Assert
        Assert.True(ReferenceEquals(varDeclSym, varRefSym));
    }
}
