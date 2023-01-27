using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using IInternalScope = Draco.Compiler.Internal.Semantics.Symbols.IScope;
using System.Collections.Immutable;

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(
                Parameter("n", NameType("int32"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x1")),
                ExpressionStatement(BlockExpression(
                    DeclarationStatement(VariableDeclaration("x2")),
                    ExpressionStatement(BlockExpression(DeclarationStatement(VariableDeclaration("x3")))))),
                ExpressionStatement(BlockExpression(
                    DeclarationStatement(VariableDeclaration("x4")),
                    ExpressionStatement(BlockExpression(DeclarationStatement(VariableDeclaration("x5")))),
                    ExpressionStatement(BlockExpression(DeclarationStatement(VariableDeclaration("x6"))))))))));

        var foo = tree.FindInChildren<FunctionDeclarationSyntax>();
        var n = tree.FindInChildren<ParameterSyntax>();
        var x1 = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var x2 = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var x3 = tree.FindInChildren<VariableDeclarationSyntax>(2);
        var x4 = tree.FindInChildren<VariableDeclarationSyntax>(3);
        var x5 = tree.FindInChildren<VariableDeclarationSyntax>(4);
        var x6 = tree.FindInChildren<VariableDeclarationSyntax>(5);

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", null, LiteralExpression(0))),
                DeclarationStatement(VariableDeclaration("x", null, BinaryExpression(NameExpression("x"), Plus, LiteralExpression(1)))),
                DeclarationStatement(VariableDeclaration("x", null, BinaryExpression(NameExpression("x"), Plus, LiteralExpression(1)))),
                DeclarationStatement(VariableDeclaration("x", null, BinaryExpression(NameExpression("x"), Plus, LiteralExpression(1))))))));

        var x0 = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var x1 = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var x2 = tree.FindInChildren<VariableDeclarationSyntax>(2);
        var x3 = tree.FindInChildren<VariableDeclarationSyntax>(3);

        var x0ref = tree.FindInChildren<NameExpressionSyntax>(0);
        var x1ref = tree.FindInChildren<NameExpressionSyntax>(1);
        var x2ref = tree.FindInChildren<NameExpressionSyntax>(2);

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
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "bar",
                ParameterList(),
                null,
                InlineFunctionBody(CallExpression(NameExpression("foo")))),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                InlineFunctionBody(CallExpression(NameExpression("foo")))),
            FunctionDeclaration(
                "baz",
                ParameterList(),
                null,
                InlineFunctionBody(CallExpression(NameExpression("foo"))))));

        var barDecl = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooDecl = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var bazDecl = tree.FindInChildren<FunctionDeclarationSyntax>(2);

        var call1 = tree.FindInChildren<CallExpressionSyntax>(0);
        var call2 = tree.FindInChildren<CallExpressionSyntax>(1);
        var call3 = tree.FindInChildren<CallExpressionSyntax>(2);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var symBar = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(barDecl));
        var symFoo = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(fooDecl));
        var symBaz = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(bazDecl));

        var refFoo1 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call1.Function));
        var refFoo2 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call2.Function));
        var refFoo3 = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(call3.Function));

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                DeclarationStatement(VariableDeclaration("y", value: BinaryExpression(NameExpression("x"), Plus, NameExpression("z")))),
                DeclarationStatement(VariableDeclaration("z"))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var yDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var zDecl = tree.FindInChildren<VariableDeclarationSyntax>(2);

        var xRef = tree.FindInChildren<NameExpressionSyntax>(0);
        var zRef = tree.FindInChildren<NameExpressionSyntax>(1);

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                ExpressionStatement(BlockExpression(
                    DeclarationStatement(VariableDeclaration("y")),
                    DeclarationStatement(VariableDeclaration("z", value: BinaryExpression(NameExpression("x"), Plus, NameExpression("y")))),
                    DeclarationStatement(VariableDeclaration("x")),
                    ExpressionStatement(BlockExpression(
                        DeclarationStatement(VariableDeclaration("k", value: BinaryExpression(NameExpression("x"), Plus, NameExpression("w")))))),
                    DeclarationStatement(VariableDeclaration("w")))),
                DeclarationStatement(VariableDeclaration("k", value: NameExpression("w")))))));

        var x1Decl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var y1Decl = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var z1Decl = tree.FindInChildren<VariableDeclarationSyntax>(2);
        var x2Decl = tree.FindInChildren<VariableDeclarationSyntax>(3);
        var k1Decl = tree.FindInChildren<VariableDeclarationSyntax>(4);
        var w1Decl = tree.FindInChildren<VariableDeclarationSyntax>(5);
        var k2Decl = tree.FindInChildren<VariableDeclarationSyntax>(6);

        var x1Ref1 = tree.FindInChildren<NameExpressionSyntax>(0);
        var y1Ref1 = tree.FindInChildren<NameExpressionSyntax>(1);
        var x2Ref1 = tree.FindInChildren<NameExpressionSyntax>(2);
        var wRefErr1 = tree.FindInChildren<NameExpressionSyntax>(3);
        var wRefErr2 = tree.FindInChildren<NameExpressionSyntax>(4);

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(
                Parameter("x", NameType("int32")),
                Parameter("x", NameType("int32"))),
            null,
            BlockFunctionBody())));

        var x1Decl = tree.FindInChildren<ParameterSyntax>(0);
        var x2Decl = tree.FindInChildren<ParameterSyntax>(1);

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
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("b", NameType("int32")),
            FunctionDeclaration(
                "b",
                ParameterList(Parameter("b", NameType("int32"))),
                NameType("int32"),
                InlineFunctionBody(NameExpression("b")))));

        var varDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var funcDecl = tree.FindInChildren<FunctionDeclarationSyntax>(0);

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
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("y", value: NameExpression("x"))))),
            VariableDeclaration("x")));

        var localVarDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var globalVarDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var varRefSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetReferencedSymbolOrNull(localVarDecl.Value!.Value));
        var varDeclSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(globalVarDecl));

        // Assert
        Assert.True(ReferenceEquals(varDeclSym, varRefSym));
    }

    [Fact]
    public void NestedLabelCanNotBeAccessed()
    {
        // func foo() {
        //     if (false) {
        //     lbl:
        //     }
        //     goto lbl;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(IfExpression(
                        condition: LiteralExpression(false),
                        then: BlockExpression(DeclarationStatement(LabelDeclaration("lbl"))))),
                    ExpressionStatement(GotoExpression("lbl"))))));

        var labelDecl = tree.FindInChildren<LabelDeclarationSyntax>(0);
        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelDeclSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetDefinedSymbolOrNull(labelDecl));
        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.False(ReferenceEquals(labelDeclSym, labelRefSym));
        Assert.False(labelDeclSym.IsError);
        Assert.True(labelRefSym.IsError);
    }

    [Fact]
    public void LabelInOtherFunctionCanNotBeAccessed()
    {
        // func foo() {
        // lbl:
        // }
        //
        // func bar() {
        //     goto lbl;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(LabelDeclaration("lbl")))),
            FunctionDeclaration(
                "bar",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(GotoExpression("lbl"))))));

        var labelDecl = tree.FindInChildren<LabelDeclarationSyntax>(0);
        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelDeclSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetDefinedSymbolOrNull(labelDecl));
        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.False(ReferenceEquals(labelDeclSym, labelRefSym));
        Assert.False(labelDeclSym.IsError);
        Assert.True(labelRefSym.IsError);
    }

    [Fact]
    public void GlobalCanNotReferenceGlobal()
    {
        // var x = 0;
        // var y = x;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", null, LiteralExpression(0)),
            VariableDeclaration("y", null, NameExpression("x"))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var xRef = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xDeclSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));
        var xRefSym = semanticModel.GetReferencedSymbol(xRef);

        // Assert
        Assert.False(ReferenceEquals(xDeclSym, xRefSym));
        Assert.False(xDeclSym.IsError);
        Assert.True(xRefSym.IsError);
    }

    [Fact]
    public void GlobalCanReferenceFunction()
    {
        // var x = foo();
        // func foo(): int32 = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", null, CallExpression(NameExpression("foo"))),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))));

        var fooDecl = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooRef = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var fooDeclSym = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(fooDecl));
        var fooRefSym = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetReferencedSymbol(fooRef));

        // Assert
        Assert.True(ReferenceEquals(fooDeclSym, fooRefSym));
    }

    [Fact]
    public void GotoToNonExistingLabel()
    {
        // func foo() {
        //     goto not_existing;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(GotoExpression("non_existing"))))));

        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.True(labelRefSym.IsError);
    }

    [Fact]
    public void GotoBreakLabelInCondition()
    {
        // func foo() {
        //     while ({ goto break; false }) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(WhileExpression(
                        condition: BlockExpression(ImmutableArray.Create(ExpressionStatement(GotoExpression("break"))), LiteralExpression(false)),
                        body: BlockExpression()))))));

        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.True(labelRefSym.IsError);
    }

    [Fact]
    public void GotoBreakLabelInInlineBody()
    {
        // func foo() {
        //     while (true) goto break;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(WhileExpression(
                        condition: LiteralExpression(false),
                        body: GotoExpression("break")))))));

        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(labelRef));

        // Assert
        Assert.False(labelRefSym.IsError);
    }

    [Fact]
    public void GotoBreakLabelInBlockBody()
    {
        // func foo() {
        //     while (true) { goto break; }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(WhileExpression(
                        condition: LiteralExpression(false),
                        body: BlockExpression(ExpressionStatement(GotoExpression("break")))))))));

        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(labelRef));

        // Assert
        Assert.False(labelRefSym.IsError);
    }

    [Fact]
    public void GotoBreakLabelOutsideOfBody()
    {
        // func foo() {
        //     while (true) {}
        //     goto break;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(WhileExpression(
                        condition: LiteralExpression(false),
                        body: BlockExpression())),
                    ExpressionStatement(GotoExpression("break"))))));

        var labelRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.True(labelRefSym.IsError);
    }

    [Fact]
    public void NestedLoopLabels()
    {
        // func foo() {
        //     while (true) {
        //         goto continue;
        //         while (true) {
        //             goto break;
        //             goto continue;
        //         }
        //         goto break;
        //     }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(WhileExpression(
                        condition: LiteralExpression(true),
                        body: BlockExpression(
                            ExpressionStatement(GotoExpression("continue")),
                            ExpressionStatement(WhileExpression(
                                condition: LiteralExpression(true),
                                body: BlockExpression(
                                    ExpressionStatement(GotoExpression("break")),
                                    ExpressionStatement(GotoExpression("continue"))))),
                            ExpressionStatement(GotoExpression("break")))))))));

        var outerContinueRef = tree.FindInChildren<GotoExpressionSyntax>(0).Target;
        var innerBreakRef = tree.FindInChildren<GotoExpressionSyntax>(1).Target;
        var innerContinueRef = tree.FindInChildren<GotoExpressionSyntax>(2).Target;
        var outerBreakRef = tree.FindInChildren<GotoExpressionSyntax>(3).Target;

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var outerContinueRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(outerContinueRef));
        var innerBreakRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(innerBreakRef));
        var innerContinueRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(innerContinueRef));
        var outerBreakRefSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetReferencedSymbol(outerBreakRef));

        // Assert
        Assert.False(outerContinueRefSym.IsError);
        Assert.False(innerBreakRefSym.IsError);
        Assert.False(innerContinueRefSym.IsError);
        Assert.False(outerBreakRefSym.IsError);
        Assert.False(ReferenceEquals(innerBreakRefSym, outerBreakRefSym));
        Assert.False(ReferenceEquals(innerContinueRefSym, outerContinueRefSym));
    }
}
