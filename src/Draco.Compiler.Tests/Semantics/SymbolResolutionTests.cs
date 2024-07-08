using System.Collections.Immutable;
using System.Reflection;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;
using Binder = Draco.Compiler.Internal.Binding.Binder;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SymbolResolutionTests : SemanticTestsBase
{
    private static PropertyInfo BinderParentProperty { get; } = typeof(Binder)
        .GetProperty("Parent", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static void AssertParentOf(Binder? parent, Binder? child)
    {
        Assert.NotNull(child);
        Assert.False(ReferenceEquals(parent, child));
        // Since the Parent property is protected, we need to access it via reflection
        var childParent = (Binder?)BinderParentProperty.GetValue(child);
        Assert.True(ReferenceEquals(childParent, parent));
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        var symFoo = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(foo));
        var symn = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDeclaredSymbol(n));
        var sym1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x1));
        var sym2 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x2));
        var sym3 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x3));
        var sym4 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x4));
        var sym5 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x5));
        var sym6 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x6));

        // Assert
        AssertParentOf(GetDefiningScope(compilation, sym2), GetDefiningScope(compilation, sym3));
        AssertParentOf(GetDefiningScope(compilation, sym1), GetDefiningScope(compilation, sym2));
        AssertParentOf(GetDefiningScope(compilation, sym4), GetDefiningScope(compilation, sym5));
        AssertParentOf(GetDefiningScope(compilation, sym4), GetDefiningScope(compilation, sym6));
        AssertParentOf(GetDefiningScope(compilation, sym1), GetDefiningScope(compilation, sym4));

        AssertParentOf(GetDefiningScope(compilation, symn), GetDefiningScope(compilation, sym1));

        AssertParentOf(GetDefiningScope(compilation, symFoo), GetDefiningScope(compilation, symn));
        Assert.True(ReferenceEquals(compilation.GetBinder(symFoo), GetDefiningScope(compilation, symn)));

        Assert.Equal(6, diagnostics.Length);
        Assert.All(diagnostics, diag => Assert.Equal(TypeCheckingErrors.CouldNotInferType, diag.Template));
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var symx0 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x0));
        var symx1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x1));
        var symx2 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x2));
        var symx3 = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x3));

        var symRefx0 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(x0ref));
        var symRefx1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(x1ref));
        var symRefx2 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(x2ref));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var symBar = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(barDecl));
        var symFoo = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooDecl));
        var symBaz = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(bazDecl));

        var refFoo1 = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call1.Function));
        var refFoo2 = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call2.Function));
        var refFoo3 = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call3.Function));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var symx = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var symy = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(yDecl));
        var symz = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(zDecl));

        var symRefx = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(xRef));
        var symRefz = GetInternalSymbol<Symbol>(semanticModel.GetReferencedSymbol(zRef));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var x1SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x1Decl));
        var y1SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(y1Decl));
        var z1SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(z1Decl));
        var x2SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(x2Decl));
        var k1SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(k1Decl));
        var w1SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(w1Decl));
        var k2SymDecl = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(k2Decl));

        var x1SymRef1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(x1Ref1));
        var y1SymRef1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(y1Ref1));
        var x2SymRef1 = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(x2Ref1));
        var wSymRef1 = semanticModel.GetReferencedSymbol(wRefErr1);
        var wSymRef2 = semanticModel.GetReferencedSymbol(wRefErr2);

        // Assert
        Assert.True(ReferenceEquals(x1SymDecl, x1SymRef1));
        Assert.True(ReferenceEquals(y1SymDecl, y1SymRef1));
        Assert.True(ReferenceEquals(x2SymDecl, x2SymRef1));
        // TODO: Maybe we should still resolve the reference, but mark it that it's something that comes later?
        // (so it is still an error)
        // It would definitely help reduce error cascading
        Assert.True(wSymRef1!.IsError);
        Assert.True(wSymRef2!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        var x1SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDeclaredSymbol(x1Decl));
        var x2SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDeclaredSymbol(x2Decl));

        // Assert
        Assert.False(x1SymDecl.IsError);
        Assert.False(x2SymDecl.IsError);
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, SymbolResolutionErrors.IllegalShadowing);
    }

    [Fact]
    public void RedefinedParameterReference()
    {
        // func foo(x: int32, x: int32) {
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(
                Parameter("x", NameType("int32")),
                Parameter("x", NameType("int32"))),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("y", null, NameExpression("x")))))));

        var x1Decl = tree.FindInChildren<ParameterSyntax>(0);
        var x2Decl = tree.FindInChildren<ParameterSyntax>(1);
        var xRef = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        var x1SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDeclaredSymbol(x1Decl));
        var x2SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDeclaredSymbol(x2Decl));
        var x2SymRef = GetInternalSymbol<ParameterSymbol>(semanticModel.GetReferencedSymbol(xRef));

        // Assert
        Assert.Equal(x2SymDecl, x2SymRef);
    }

    [Fact]
    public void GenericParameterRedefinitionError()
    {
        // func foo<T, T>() {}

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            GenericParameterList(GenericParameter("T"), GenericParameter("T")),
            ParameterList(),
            null,
            BlockFunctionBody())));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, SymbolResolutionErrors.IllegalShadowing);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        var varSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(varDecl));
        var funcSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(funcDecl));

        // Assert
        Assert.False(varSym.IsError);
        Assert.False(funcSym.IsError);
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, SymbolResolutionErrors.IllegalShadowing);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var varRefSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetReferencedSymbol(localVarDecl.Value!.Value));
        var varDeclSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(globalVarDecl));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelDeclSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetDeclaredSymbol(labelDecl));
        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.False(ReferenceEquals(labelDeclSym, labelRefSym));
        Assert.False(labelDeclSym.IsError);
        Assert.True(labelRefSym!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelDeclSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetDeclaredSymbol(labelDecl));
        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.False(ReferenceEquals(labelDeclSym, labelRefSym));
        Assert.False(labelDeclSym.IsError);
        Assert.True(labelRefSym!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var xDeclSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var xRefSym = semanticModel.GetReferencedSymbol(xRef);

        // TODO: Should see it, but should report illegal reference error
        // Assert
        Assert.False(ReferenceEquals(xDeclSym, xRefSym));
        Assert.False(xDeclSym.IsError);
        Assert.True(xRefSym!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var fooDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooDecl));
        var fooRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooRef));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.True(labelRefSym!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(labelRef));

        // Assert
        Assert.False(labelRefSym.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(labelRef));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(labelRef));

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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelRefSym = semanticModel.GetReferencedSymbol(labelRef);

        // Assert
        Assert.True(labelRefSym!.IsError);
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var outerContinueRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(outerContinueRef));
        var innerBreakRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(innerBreakRef));
        var innerContinueRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(innerContinueRef));
        var outerBreakRefSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetReferencedSymbol(outerBreakRef));

        // Assert
        Assert.False(outerContinueRefSym.IsError);
        Assert.False(innerBreakRefSym.IsError);
        Assert.False(innerContinueRefSym.IsError);
        Assert.False(outerBreakRefSym.IsError);
        Assert.False(ReferenceEquals(innerBreakRefSym, outerBreakRefSym));
        Assert.False(ReferenceEquals(innerContinueRefSym, outerContinueRefSym));
    }

    [Fact]
    public void ModuleIsIllegalInExpressionContext()
    {
        // func foo() {
        //     var a = System;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("a", null, NameExpression("System")))))));

        var varDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var moduleRef = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var localSymbol = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(varDecl));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(moduleRef));

        // Assert
        Assert.True(localSymbol.Type.IsError);
        Assert.False(systemSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalExpression);
    }

    [Fact]
    public void FunctionGroupIsIllegalInExpressionContext()
    {
        // func main()
        // {
        //     foo
        // }
        // func foo() { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(NameExpression("foo")))),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody())));

        var funcGroupRef = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalExpression);
    }

    [Fact]
    public void ImportPointsToNonExistingModuleInCompilationUnit()
    {
        // import Nonexisting;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("Nonexisting")));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ImportPointsToNonExistingModuleInFunctionBody()
    {
        // func foo() {
        //     import Nonexisting;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("Nonexisting"))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ImportIsNotAtTheTopOfCompilationUnit()
    {
        // func foo() {}
        // import System;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody()),
            ImportDeclaration("System")));

        var importPath = tree.FindInChildren<ImportPathSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(importPath));

        // Assert
        Assert.False(systemSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.ImportNotAtTop);
    }

    [Fact]
    public void ImportIsNotAtTheTopOfFunctionBody()
    {
        // func foo() {
        //     var x = 0;
        //     import System;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", null, LiteralExpression(0))),
                DeclarationStatement(ImportDeclaration("System"))))));

        var importPath = tree.FindInChildren<ImportPathSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(importPath));

        // Assert
        Assert.False(systemSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.ImportNotAtTop);
    }

    [Fact]
    public void ModuleAsReturnType()
    {
        // func foo(): System = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("System"),
            InlineFunctionBody(
                LiteralExpression(0)))));

        var returnTypeSyntax = tree.FindInChildren<NameTypeSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var returnTypeSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(returnTypeSyntax));

        // Assert
        Assert.NotNull(returnTypeSymbol);
        Assert.False(returnTypeSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.IllegalModuleType);
    }

    [Fact]
    public void ModuleAsVariableType()
    {
        // func foo() {
        //     var x: System = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("System"), LiteralExpression(0)))))));

        var varTypeSyntax = tree.FindInChildren<NameTypeSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var varTypeSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(varTypeSyntax));

        // Assert
        Assert.NotNull(varTypeSymbol);
        Assert.False(varTypeSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.IllegalModuleType);
    }

    [Fact]
    public void VisibleElementFullyQualified()
    {
        // func main(){
        //   FooModule.foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "foo")))))),
            ToPath("Tests", "main.draco"));

        // internal func foo(): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                Api.Semantics.Visibility.Internal,
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        var fooDecl = foo.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooCall = main.FindInChildren<CallExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var mainModel = compilation.GetSemanticModel(main);
        var fooModel = compilation.GetSemanticModel(foo);

        var diags = mainModel.Diagnostics;

        var fooCallSymbol = GetInternalSymbol<FunctionSymbol>(mainModel.GetReferencedSymbol(fooCall));
        var fooDeclSymbol = GetInternalSymbol<FunctionSymbol>(fooModel.GetDeclaredSymbol(fooDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(fooDeclSymbol, fooCallSymbol);
    }

    [Fact]
    public void NotVisibleElementFullyQualified()
    {
        // func main(){
        //   FooModule.foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "foo")))))),
            ToPath("Tests", "main.draco"));

        // func foo(): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void VisibleElementFullyQualifiedInCodeDefinedModule()
    {
        // func main() {
        //   FooModule.foo();
        // }
        //
        // module FooModule{
        //   internal func foo(): int32 = 0;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "foo"))))),
            ModuleDeclaration(
                "FooModule",
                FunctionDeclaration(
                    Api.Semantics.Visibility.Internal,
                    "foo",
                    ParameterList(),
                    NameType("int32"),
                    InlineFunctionBody(LiteralExpression(0))))));

        var fooDecl = main.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooCall = main.FindInChildren<CallExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        var fooCallSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooCall));
        var fooDeclSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(fooDeclSymbol, fooCallSymbol);
    }

    [Fact]
    public void NotVisibleElementFullyQualifiedInCodeDefinedModule()
    {
        // func main() {
        //   FooModule.foo();
        // }
        //
        // module FooModule {
        //   func foo(): int32 = 0;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "foo"))))),
            ModuleDeclaration(
                "FooModule",
                FunctionDeclaration(
                    "foo",
                    ParameterList(),
                    NameType("int32"),
                    InlineFunctionBody(LiteralExpression(0))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var fooCall = main.FindInChildren<MemberExpressionSyntax>(0);

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooCallSymbol = GetInternalSymbol<Symbol>(semanticModel.GetReferencedSymbol(fooCall));

        // Assert
        Assert.Single(diags);
        Assert.True(fooCallSymbol.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void NotVisibleGlobalVariableFullyQualified()
    {
        // func main() {
        //   var x = BarModule.bar;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", type: null, value: MemberExpression(NameExpression("BarModule"), "bar")))))),
            ToPath("Tests", "main.draco"));

        // var bar = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("bar", type: null, value: LiteralExpression(0))),
           ToPath("Tests", "BarModule", "bar.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void InternalElementImportedFromDifferentAssembly()
    {
        // import FooModule;
        // func main() {
        //   Foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("Foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                internal static void Foo() { }
            }
            """);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void InternalElementFullyQualifiedFromDifferentAssembly()
    {
        // func main() {
        //   FooModule.Foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("FooModule"), "Foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                internal static void Foo() { }
            }
            """);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void VisibleElementImported()
    {
        // import FooModule;
        // func main(){
        //   foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo")))))),
            ToPath("Tests", "main.draco"));

        // internal func foo(): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                Api.Semantics.Visibility.Internal,
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var fooDecl = foo.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooCall = main.FindInChildren<CallExpressionSyntax>(0);

        var mainModel = compilation.GetSemanticModel(main);
        var fooModel = compilation.GetSemanticModel(foo);

        var diags = mainModel.Diagnostics;

        var fooCallSymbol = GetInternalSymbol<FunctionSymbol>(mainModel.GetReferencedSymbol(fooCall));
        var fooDeclSymbol = GetInternalSymbol<FunctionSymbol>(fooModel.GetDeclaredSymbol(fooDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(fooDeclSymbol, fooCallSymbol);
    }

    [Fact]
    public void NotVisibleElementImported()
    {
        // import FooModule;
        // func main(){
        //   foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo")))))),
            ToPath("Tests", "main.draco"));

        // func foo(): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "FooModule", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void VisibleElementImportedInCodeDefinedModule()
    {
        // import FooModule;
        // func main(){
        //   foo();
        // }
        //
        // module FooModule{
        //   internal func foo(): int32 = 0;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"))))),
            ModuleDeclaration(
                "FooModule",
                FunctionDeclaration(
                    Api.Semantics.Visibility.Internal,
                    "foo",
                    ParameterList(),
                    NameType("int32"),
                    InlineFunctionBody(LiteralExpression(0))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var fooDecl = main.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooCall = main.FindInChildren<CallExpressionSyntax>(0);

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        var fooCallSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooCall));
        var fooDeclSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(fooDeclSymbol, fooCallSymbol);
    }

    [Fact]
    public void NotVisibleElementImportedInCodeDefinedModule()
    {
        // import FooModule;
        // func main(){
        //   foo();
        // }
        //
        // module FooModule{
        //   func foo(): int32 = 0;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"))))),
            ModuleDeclaration(
                "FooModule",
                FunctionDeclaration(
                    "foo",
                    ParameterList(),
                    NameType("int32"),
                    InlineFunctionBody(LiteralExpression(0))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var fooCall = main.FindInChildren<NameExpressionSyntax>(0);

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooCallSymbol = GetInternalSymbol<Symbol>(semanticModel.GetReferencedSymbol(fooCall));

        // Assert
        Assert.Single(diags);
        Assert.True(fooCallSymbol.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ElementFromTheSameModuleButDifferentFile()
    {
        // func main(){
        //   foo();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo")))))),
            ToPath("Tests", "main.draco"));

        // func foo(): int32 = 0;

        var foo = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0)))),
           ToPath("Tests", "foo.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main, foo],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var fooDecl = foo.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooCall = main.FindInChildren<CallExpressionSyntax>(0);

        var mainModel = compilation.GetSemanticModel(main);
        var fooModel = compilation.GetSemanticModel(foo);

        var diags = mainModel.Diagnostics;

        var fooCallSymbol = GetInternalSymbol<FunctionSymbol>(mainModel.GetReferencedSymbol(fooCall));
        var fooDeclSymbol = GetInternalSymbol<FunctionSymbol>(fooModel.GetDeclaredSymbol(fooDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(fooDeclSymbol, fooCallSymbol);
    }

    [Fact]
    public void SyntaxTreeOutsideOfRoot()
    {
        // func main() { }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody())),
            ToPath("NotRoot", "main.draco"));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var diags = compilation.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.FilePathOutsideOfRootPath);
    }

    [Fact]
    public void InCodeModuleImports()
    {
        // import System.Text;
        //
        // module FooModule {
        //     import System.Console;
        //
        //     func bar()
        //     {
        //         var sb = StringBuilder(); // OK
        //         WriteLine(sb.ToString()); // OK
        //     }
        // }
        //
        // func baz()
        // {
        //     var sb = StringBuilder(); // OK
        //     WriteLine(); // ERROR
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Text"),
            ModuleDeclaration(
                "FooModule",
                ImportDeclaration("System", "Console"),
                FunctionDeclaration(
                    "bar",
                    ParameterList(),
                    null,
                    BlockFunctionBody(
                        DeclarationStatement(VariableDeclaration("sb", null, CallExpression(NameExpression("StringBuilder")))),
                        ExpressionStatement(CallExpression(NameExpression("WriteLine"), CallExpression(MemberExpression(NameExpression("sb"), "ToString"))))))),
            FunctionDeclaration(
                "baz",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("sb", null, CallExpression(NameExpression("StringBuilder")))),
                    ExpressionStatement(CallExpression(NameExpression("WriteLine")))))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray(),
            rootModulePath: ToPath("Tests"));

        var writeLineCall = main.FindInChildren<CallExpressionSyntax>(4).Function;

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var writeLineCallSymbol = GetInternalSymbol<Symbol>(semanticModel.GetReferencedSymbol(writeLineCall));

        // Assert
        Assert.Single(diags);
        Assert.True(writeLineCallSymbol.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void UndefinedTypeInReturnType()
    {
        // func foo(): unknown { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("unknown"),
            BlockFunctionBody())));

        var returnTypeSyntax = tree.FindInChildren<NameTypeSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var returnTypeSymbol = semanticModel.GetReferencedSymbol(returnTypeSyntax);

        // Assert
        Assert.NotNull(returnTypeSymbol);
        Assert.True(returnTypeSymbol.IsError);
        Assert.Equal(2, diags.Length);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
        AssertDiagnostic(diags, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void UndefinedTypeInParameterType()
    {
        // func foo(x: unknown) { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(Parameter("x", NameType("unknown"))),
            null,
            BlockFunctionBody())));

        var paramTypeSyntax = tree.FindInChildren<NameTypeSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var paramTypeSymbol = semanticModel.GetReferencedSymbol(paramTypeSyntax);

        // Assert
        Assert.NotNull(paramTypeSymbol);
        Assert.True(paramTypeSymbol.IsError);
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ReadingAndSettingStaticFieldFullyQualified()
    {
        // func main(){
        //   FooModule.foo = 5;
        //   var x = FooModule.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("FooModule"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo = 0;
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);
        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<GlobalSymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void ReadingAndSettingStaticFieldImported()
    {
        // import FooModule;
        // func main(){
        //   foo = 5;
        //   var x = foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(NameExpression("foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, NameExpression("foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo = 0;
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);
        var fooNameRef = main.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetReferencedSymbol(fooNameRef));
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void ReadingAndSettingNonStaticField()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo = 5;
        //   var x = fooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("fooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int foo = 0;
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void SettingReadonlyStaticField()
    {
        // func main(){
        //   FooModule.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static readonly int foo = 0;
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<GlobalSymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo);
    }

    [Fact]
    public void SettingConstantField()
    {
        // func main(){
        //   FooModule.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5)))))));


        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public const int foo = 0;
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<GlobalSymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo);
    }

    [Fact]
    public void SettingReadonlyNonStaticField()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public readonly int foo = 0;
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, FlowAnalysisErrors.ImmutableVariableCanNotBeAssignedTo);
    }

    [Fact]
    public void ReadingNonExistingNonStaticField()
    {
        // func main(){
        //   var fooType = FooType();
        //   var x = fooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("fooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType { }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooTypeSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");

        // Assert
        Assert.Single(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.MemberNotFound);
    }

    [Fact]
    public void SettingNonExistingNonStaticField()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType { }
            """);

        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");

        // Assert
        Assert.Single(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.MemberNotFound);
    }

    [Fact]
    public void SettingNonExistingStaticField()
    {
        // func main(){
        //   FooModule.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule { }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooModuleSym = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef));
        var fooModuleDecl = GetMetadataSymbol(compilation, null, "FooModule");

        // Assert
        Assert.Single(diags);
        Assert.False(fooModuleSym.IsError);
        Assert.Same(fooModuleSym, fooModuleDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ReadingNonExistingStaticField()
    {
        // func main(){
        //   var x = FooModule.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("FooModule"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule { }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);
        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooModuleSym = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef));
        var fooModuleDecl = GetMetadataSymbol(compilation, null, "FooModule");

        // Assert
        Assert.Single(diags);
        Assert.False(fooModuleSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooModuleSym, fooModuleDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void ReadingAndSettingStaticPropertyFullyQualified()
    {
        // func main(){
        //   FooModule.foo = 5;
        //   var x = FooModule.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("FooModule"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo { get; set; }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);
        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void ReadingAndSettingStaticPropertyImported()
    {
        // import FooModule;
        // func main(){
        //   foo = 5;
        //   var x = foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("FooModule"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(NameExpression("foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, NameExpression("foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo { get; set; }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);
        var fooAssignRef = main.FindInChildren<BinaryExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetInternalSymbol<PropertySymbol>(semanticModel.GetReferencedSymbol(fooAssignRef));
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void ReadingAndSettingNonStaticProperty()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo = 5;
        //   var x = fooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("fooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int foo { get; set; }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);

    }

    [Fact]
    public void SettingGetOnlyStaticProperty()
    {
        // func main(){
        //   FooModule.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), Assign, LiteralExpression(5)))))));


        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo { get; }
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.CannotSetGetOnlyProperty);
    }

    [Fact]
    public void GettingSetOnlyStaticProperty()
    {
        // func main(){
        //   var x = FooModule.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("FooModule"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo { set { } }
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.CannotGetSetOnlyProperty);
    }

    [Fact]
    public void SettingGetOnlyNonStaticProperty()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int foo { get; }
            }
            """);

        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.CannotSetGetOnlyProperty);
    }

    [Fact]
    public void GettingSetOnlyNonStaticProperty()
    {
        // func main(){
        //   var fooType = FooType();
        //   var x = fooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("fooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int foo { set { } }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.CannotGetSetOnlyProperty);
    }

    [Fact]
    public void CompoundAssignmentNonStaticProperty()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo += 2;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("fooType"), "foo"), PlusAssign, LiteralExpression(2)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int foo { get; set; }
            }
            """);

        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void CompoundAssignmentStaticProperty()
    {
        // func main(){
        //   FooModule.foo += 2;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(BinaryExpression(MemberExpression(NameExpression("FooModule"), "foo"), PlusAssign, LiteralExpression(2)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class FooModule{
                public static int foo { get; set; }
            }
            """);

        var fooModuleRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(fooModuleRef)), "foo");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooModule", "foo");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void ReadingAndSettingIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType[0] = 5;
        //   var x = fooType[0];
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(NameExpression("fooType"), LiteralExpression(0)), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, IndexExpression(NameExpression("fooType"), LiteralExpression(0))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int this[int index]
                {
                    get => index * 2;
                    set { }
                }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<IndexExpressionSyntax>(0).Indexed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "Item");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "Item");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void SettingGetOnlyIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType[0] = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(NameExpression("fooType"), LiteralExpression(0)), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int this[int index] => index * 2;
            }
            """);

        var fooAssignRef = main.FindInChildren<BinaryExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetInternalSymbol<ErrorPropertySymbol>(semanticModel.GetReferencedSymbol(fooAssignRef));

        // Assert
        Assert.Single(diags);
        Assert.True(fooSym.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoSettableIndexerInType);
    }

    [Fact]
    public void GettingSetOnlyIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   var x = fooType[0];
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, IndexExpression(NameExpression("fooType"), LiteralExpression(0))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public int this[int index] { set { } }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<IndexExpressionSyntax>(0).Indexed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "Item");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "Item");

        // Assert
        Assert.Single(diags);
        Assert.False(fooSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fooSym, fooDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoGettableIndexerInType);
    }

    [Fact]
    public void ReadingAndSettingMemberAccessIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo[0] = 5;
        //   var x = fooType.foo[0];
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(MemberExpression(NameExpression("fooType"), "foo"), LiteralExpression(0)), Assign, LiteralExpression(5))),
                    DeclarationStatement(VariableDeclaration("x", null, IndexExpression(MemberExpression(NameExpression("fooType"), "foo"), LiteralExpression(0))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public Foo foo = new Foo();
            }
            public class Foo{
                public int this[int index]
                {
                    get => index * 2;
                    set { }
                }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var indexSym = GetMemberSymbol<PropertySymbol>(fooSym.Type, "Item");
        var indexDecl = GetMetadataSymbol(compilation, null, "Foo", "Item");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooSym.IsError);
        Assert.False(indexSym.IsError);
        Assert.Same(indexSym, indexDecl);
    }

    [Fact]
    public void SettingGetOnlyMemberAccessIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   fooType.foo[0] = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(MemberExpression(NameExpression("fooType"), "foo"), LiteralExpression(0)), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public Foo foo = new Foo();
            }
            public class Foo{
                public int this[int index] => index * 2;
            }
            """);

        var fooAssignRef = main.FindInChildren<BinaryExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetInternalSymbol<ErrorPropertySymbol>(semanticModel.GetReferencedSymbol(fooAssignRef));

        // Assert
        Assert.Single(diags);
        Assert.True(fooSym.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoSettableIndexerInType);
    }

    [Fact]
    public void GettingSetOnlyMemberAccessIndexer()
    {
        // func main(){
        //   var fooType = FooType();
        //   var x = fooType.foo[0];
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("fooType", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, IndexExpression(MemberExpression(NameExpression("fooType"), "foo"), LiteralExpression(0))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType{
                public Foo foo = new Foo();
            }
            public class Foo{
                public int this[int index] { set { } }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooSym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "foo");
        var indexSym = GetMemberSymbol<PropertySymbol>(fooSym.Type, "Item");
        var indexDecl = GetMetadataSymbol(compilation, null, "Foo", "Item");

        // Assert
        Assert.Single(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooSym.IsError);
        Assert.False(indexSym.IsError);
        Assert.Same(indexSym, indexDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoGettableIndexerInType);
    }

    [Fact]
    public void GettingNonExistingIndexer()
    {
        // func main(){
        //   var foo = FooType();
        //   var x = foo[0];
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, IndexExpression(NameExpression("foo"), LiteralExpression(0))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType { }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<IndexExpressionSyntax>(0).Indexed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooTypeSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");

        // Assert
        Assert.Single(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoGettableIndexerInType);
    }

    [Fact]
    public void SettingNonExistingIndexer()
    {
        // func main(){
        //   var foo = FooType();
        //   foo[0] = 5;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(NameExpression("foo"), LiteralExpression(0)), Assign, LiteralExpression(5)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType { }
            """);

        var fooAssignRef = main.FindInChildren<BinaryExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetInternalSymbol<ErrorPropertySymbol>(semanticModel.GetReferencedSymbol(fooAssignRef));

        // Assert
        Assert.Single(diags);
        Assert.True(fooSym.IsError);
        AssertDiagnostic(diags, SymbolResolutionErrors.NoSettableIndexerInType);
    }

    [Fact]
    public void CompoundAssignmentIndexer()
    {
        // func main(){
        //   var foo = FooType();
        //   foo[0] += 2;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("FooType")))),
                    ExpressionStatement(BinaryExpression(IndexExpression(NameExpression("foo"), LiteralExpression(0)), PlusAssign, LiteralExpression(2)))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType
            {
                public int this[int index]
                {
                    get => index * 2;
                    set { }
                }
            }
            """);

        var fooTypeRef = main.FindInChildren<IndexExpressionSyntax>(0).Indexed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooSym = GetMemberSymbol<PropertySymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type, "Item");
        var fooDecl = GetMetadataSymbol(compilation, null, "FooType", "Item");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooSym.IsError);
        Assert.Same(fooSym, fooDecl);
    }

    [Fact]
    public void NestedTypeWithStaticParentInTypeContextAndConstructorFullyQualified()
    {
        // func foo(): ParentType.FooType = ParentType.FooType();

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                MemberType(NameType("ParentType"), "FooType"),
                InlineFunctionBody(
                    CallExpression(MemberExpression(NameExpression("ParentType"), "FooType"))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class ParentType
            {
                public class FooType { }
            }
            """);

        var parentTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetMemberSymbol<TypeSymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(parentTypeRef)), "FooType");
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeDecl, fooTypeSym);
    }

    [Fact]
    public void NestedTypeWithStaticParentInTypeContextAndConstructorImported()
    {
        // import ParentType;
        // func foo(): FooType = FooType();

        var main = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("ParentType"),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("FooType"),
                InlineFunctionBody(
                    CallExpression(NameExpression("FooType"))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class ParentType{
                public class FooType { }
            }
            """);

        var fooTypeRef = main.FindInChildren<TypeSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef));
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeDecl, fooTypeSym);
    }

    [Fact]
    public void NestedTypeWithNonStaticParentInTypeContextAndConstructor()
    {
        // func foo(): ParentType.FooType = ParentType.FooType();

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                MemberType(NameType("ParentType"), "FooType"),
                InlineFunctionBody(
                    CallExpression(MemberExpression(NameExpression("ParentType"), "FooType"))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType
            {
                public class FooType { }
            }
            """);

        var parentTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetMemberSymbol<TypeSymbol>(GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(parentTypeRef)), "FooType");
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeDecl, fooTypeSym);
    }

    [Fact]
    public void NestedTypeWithStaticParentStaticMemberAccess()
    {
        // func foo()
        // {
        //   var x = ParentType.FooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(MemberExpression(NameExpression("ParentType"), "FooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class ParentType
            {
                public class FooType
                {
                    public static int foo = 0;
                }
            }
            """);

        var parentTypeRef = main.FindInChildren<MemberExpressionSyntax>(1).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetMemberSymbol<TypeSymbol>(GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(parentTypeRef)), "FooType");
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeDecl, fooTypeSym);
    }

    [Fact]
    public void NestedTypeWithNonStaticParentStaticMemberAccess()
    {
        // func foo()
        // {
        //   var x = ParentType.FooType.foo;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(MemberExpression(NameExpression("ParentType"), "FooType"), "foo")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType
            {
                public class FooType
                {
                    public static int foo = 0;
                }
            }
            """);

        var parentTypeRef = main.FindInChildren<MemberExpressionSyntax>(1).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetMemberSymbol<TypeSymbol>(GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(parentTypeRef)), "FooType");
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeDecl, fooTypeSym);
    }

    [Fact]
    public void NestedTypeWithStaticParentNonStaticMemberAccess()
    {
        // func foo()
        // {
        //   var foo = ParentType.FooType();
        //   var x = foo.member;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(MemberExpression(NameExpression("ParentType"), "FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("foo"), "member")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public static class ParentType
            {
                public class FooType
                {
                    public int member = 0;
                }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(1).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooTypeSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
    }

    [Fact]
    public void NestedTypeWithNonStaticParentNonStaticMemberAccess()
    {
        // func foo()
        // {
        //   var foo = ParentType.FooType();
        //   var x = foo.member;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(MemberExpression(NameExpression("ParentType"), "FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("foo"), "member")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType
            {
                public class FooType
                {
                    public int member = 0;
                }
            }
            """);

        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(1);
        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(1).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fooTypeSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "ParentType", "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(xSym.IsError);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
    }

    [Fact]
    public void GenericFunction()
    {
        // func identity<T>(x: T): T = x;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            GenericParameterList(GenericParameter("T")),
            ParameterList(Parameter("x", NameType("T"))),
            NameType("T"),
            InlineFunctionBody(NameExpression("x")))));

        var functionSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var genericTypeSyntax = tree.FindInChildren<GenericParameterSyntax>(0);
        var paramTypeSyntax = tree.FindInChildren<NameTypeSyntax>(0);
        var returnTypeSyntax = tree.FindInChildren<NameTypeSyntax>(1);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var functionSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(functionSyntax));
        var genericTypeSymbol = GetInternalSymbol<TypeParameterSymbol>(semanticModel.GetDeclaredSymbol(genericTypeSyntax));
        var paramTypeSymbol = GetInternalSymbol<TypeParameterSymbol>(semanticModel.GetReferencedSymbol(paramTypeSyntax));
        var returnTypeSymbol = GetInternalSymbol<TypeParameterSymbol>(semanticModel.GetReferencedSymbol(returnTypeSyntax));

        // Assert
        Assert.True(functionSymbol.IsGenericDefinition);
        Assert.NotNull(genericTypeSymbol);
        Assert.Same(genericTypeSymbol, paramTypeSymbol);
        Assert.Same(genericTypeSymbol, returnTypeSymbol);
        Assert.Empty(diags);
    }

    [Fact]
    public void InheritanceFromObject()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType { }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Single(fooTypeSym.ImmediateBaseTypes);
        Assert.Equal("System.Object", fooTypeSym.ImmediateBaseTypes[0].FullName);
    }

    [Fact]
    public void InheritanceFromTypeDefinition()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType { }
            public class FooType : ParentType { }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");
        var parentTypeDecl = GetMetadataSymbol(compilation, null, "ParentType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Single(fooTypeSym.ImmediateBaseTypes);
        Assert.Equal(parentTypeDecl, fooTypeSym.ImmediateBaseTypes[0]);
    }

    [Fact]
    public void InheritanceFromNestedTypeReference()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var baseStream = CompileCSharpToStream("""
            public class ParentType
            {
                public class BaseType { }
            }
            """, "Base.dll");

        var baseRef = MetadataReference.FromPeStream(baseStream);

        baseStream.Position = 0;

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType : ParentType.BaseType { }
            """, aditionalReferences: new[] { baseStream });

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(baseRef)
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");
        var baseTypeDecl = GetMetadataSymbol(compilation, "Base.dll", "ParentType", "BaseType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Single(fooTypeSym.ImmediateBaseTypes);
        Assert.Equal(baseTypeDecl, fooTypeSym.ImmediateBaseTypes[0]);
    }

    [Fact]
    public void InheritanceFromTypeSpecification()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType<T> { }
            public class FooType : ParentType<int> { }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");
        var parentTypeDecl = GetMetadataSymbol(compilation, null, "ParentType`1");
        var baseTypeSym = fooTypeSym.ImmediateBaseTypes[0];

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Single(fooTypeSym.ImmediateBaseTypes);
        Assert.True(baseTypeSym.IsGenericInstance);
        Assert.False(baseTypeSym.IsGenericDefinition);
        Assert.Same(parentTypeDecl, baseTypeSym.GenericDefinition);
        Assert.Single(baseTypeSym.GenericArguments);
        Assert.Same(compilation.WellKnownTypes.SystemInt32, baseTypeSym.GenericArguments[0]);
    }

    [Fact]
    public void InheritingInterfacesFromTypeDefinition()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public interface ParentInterface { }
            public class FooType : ParentInterface { }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");
        var parentInterfaceDecl = GetMetadataSymbol(compilation, null, "ParentInterface");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Equal(2, fooTypeSym.ImmediateBaseTypes.Length);
        Assert.Equal("System.Object", fooTypeSym.ImmediateBaseTypes[0].FullName);
        Assert.Equal(parentInterfaceDecl, fooTypeSym.ImmediateBaseTypes[1]);
    }

    [Fact]
    public void InheritingInterfacesFromTypeReference()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class FooType : System.ICloneable
            {
                public object Clone() => new object();
            }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Equal(2, fooTypeSym.ImmediateBaseTypes.Length);
        Assert.Equal("System.Object", fooTypeSym.ImmediateBaseTypes[0].FullName);
        Assert.Equal("System.ICloneable", fooTypeSym.ImmediateBaseTypes[1].FullName);
    }

    [Fact]
    public void InheretingInterfaceFromTypeSpecification()
    {
        // func foo()
        // {
        //   var foo = FooType();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression((NameExpression("FooType")))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public interface ParentInterface<T> { }
            public class FooType : ParentInterface<int> { }
            """);

        var fooTypeRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var fooTypeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).ReturnType;
        var fooTypeDecl = GetMetadataSymbol(compilation, null, "FooType");
        var parentInterfaceDecl = GetMetadataSymbol(compilation, null, "ParentInterface`1");
        var baseInterfaceSym = fooTypeSym.ImmediateBaseTypes[^1];

        // Assert
        Assert.Empty(diags);
        Assert.False(fooTypeSym.IsError);
        Assert.Same(fooTypeSym, fooTypeDecl);
        Assert.Equal(2, fooTypeSym.ImmediateBaseTypes.Length);
        Assert.Equal("System.Object", fooTypeSym.ImmediateBaseTypes[0].FullName);
        Assert.True(baseInterfaceSym.IsGenericInstance);
        Assert.False(baseInterfaceSym.IsGenericDefinition);
        Assert.Same(parentInterfaceDecl, baseInterfaceSym.GenericDefinition);
        Assert.Single(baseInterfaceSym.GenericArguments);
        Assert.Same(compilation.WellKnownTypes.SystemInt32, baseInterfaceSym.GenericArguments[0]);
    }

    [Fact]
    public void AccessingMemberOfBaseType()
    {
        // func foo()
        // {
        //   var foo = FooType();
        //   var x = foo.Field;
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("FooType")))),
                    DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("foo"), "Field")))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class ParentType
            {
                public int Field = 5;
            }
            public class FooType : ParentType { }
            """);

        var fooTypeRef = main.FindInChildren<MemberExpressionSyntax>(0).Accessed;
        var xDecl = main.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<VariableSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var fieldSym = GetMemberSymbol<FieldSymbol>(GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(fooTypeRef)).Type.ImmediateBaseTypes[0], "Field");
        var fieldDecl = GetMetadataSymbol(compilation, null, "ParentType", "Field");

        // Assert
        Assert.Empty(diags);
        Assert.False(fieldSym.IsError);
        Assert.False(xSym.IsError);
        Assert.Same(fieldSym, fieldDecl);
    }

    [Fact]
    public void ImplicitOverrideFunction()
    {
        // func foo()
        // {
        //   var foo = Derived();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("Derived"))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Base
            {
                public virtual Base Clone() => this;
            }

            public class Derived : Base
            {
                public override Base Clone() => this;
            }
            """);

        var derivedRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var derivedSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(derivedRef)).ReturnType;
        var derivedDecl = GetMetadataSymbol(compilation, null, "Derived");
        var nonObjectSymbols = derivedSym.NonSpecialMembers.Where(x => x.ContainingSymbol?.FullName != "System.Object");

        // Assert
        Assert.Empty(diags);
        Assert.Same(derivedDecl, derivedSym);
        Assert.Single(nonObjectSymbols);
        Assert.Equal("Derived.Clone", nonObjectSymbols.First().FullName);
    }

    [Fact]
    public void ExplicitOverrideFunctionInSameAssembly()
    {
        // func foo()
        // {
        //   var foo = Derived();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("Derived"))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Base
            {
                public virtual Base Clone(int parameter) => this;
            }

            public class Derived : Base
            {
                public override Derived Clone(int parameter) => this;
            }
            """);

        var derivedRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var derivedSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(derivedRef)).ReturnType;
        var derivedDecl = GetMetadataSymbol(compilation, null, "Derived");
        var nonObjectSymbols = derivedSym.NonSpecialMembers.Where(x => x.ContainingSymbol?.FullName != "System.Object");

        // Assert
        Assert.Empty(diags);
        Assert.Same(derivedDecl, derivedSym);
        Assert.Single(nonObjectSymbols);
        Assert.Equal("Derived.Clone", nonObjectSymbols.First().FullName);
    }

    [Fact]
    public void ExplicitOverrideFunctionInDifferentAssembly()
    {
        // func foo()
        // {
        //   var foo = Derived();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("Derived"))))))));

        var baseStream = CompileCSharpToStream("""
            public class Base
            {
                public virtual Base Clone(int parameter) => this;
            }
            """, "Base.dll");

        var baseRef = MetadataReference.FromPeStream(baseStream);

        baseStream.Position = 0;

        var fooRef = CompileCSharpToMetadataRef("""
            public class Derived : Base
            {
                public override Derived Clone(int parameter) => this;
            }
            """, aditionalReferences: new Stream[] { baseStream });

        var derivedRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .Append(baseRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var derivedSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(derivedRef)).ReturnType;
        var derivedDecl = GetMetadataSymbol(compilation, null, "Derived");
        var nonObjectSymbols = derivedSym.NonSpecialMembers.Where(x => x.ContainingSymbol?.FullName != "System.Object");

        // Assert
        Assert.Empty(diags);
        Assert.Same(derivedDecl, derivedSym);
        Assert.Single(nonObjectSymbols);
        Assert.Equal("Derived.Clone", nonObjectSymbols.First().FullName);
    }

    [Fact]
    public void ImplicitOverrideProperty()
    {
        // func foo()
        // {
        //   var foo = Derived();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("Derived"))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Base
            {
                public virtual Base Clone => this;
            }

            public class Derived : Base
            {
                public override Base Clone => this;
            }
            """);

        var derivedRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var derivedSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(derivedRef)).ReturnType;
        var derivedDecl = GetMetadataSymbol(compilation, null, "Derived");
        var nonObjectSymbols = derivedSym.NonSpecialMembers.Where(x => x.ContainingSymbol?.FullName != "System.Object");

        // Assert
        Assert.Empty(diags);
        Assert.Same(derivedDecl, derivedSym);
        Assert.Single(nonObjectSymbols);
        Assert.Equal("Derived.Clone", nonObjectSymbols.First().FullName);
    }

    [Fact]
    public void ExplicitOverrideProperty()
    {
        // func foo()
        // {
        //   var foo = Derived();
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("foo", null, CallExpression(NameExpression("Derived"))))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Base
            {
                public virtual Base Clone => this;
            }

            public class Derived : Base
            {
                public override Derived Clone => this;
            }
            """);

        var derivedRef = main.FindInChildren<CallExpressionSyntax>(0).Function;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;
        var derivedSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(derivedRef)).ReturnType;
        var derivedDecl = GetMetadataSymbol(compilation, null, "Derived");
        var nonObjectSymbols = derivedSym.NonSpecialMembers.Where(x => x.ContainingSymbol?.FullName != "System.Object");

        // Assert
        Assert.Empty(diags);
        Assert.Same(derivedDecl, derivedSym);
        Assert.Single(nonObjectSymbols);
        Assert.Equal("Derived.Clone", nonObjectSymbols.First().FullName);
    }

    [Fact]
    public void InCodeModules()
    {
        // module Foo {
        //     public func bar() {}
        // }
        //
        // func main() {
        //     Foo.bar();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ModuleDeclaration(
                "Foo",
                FunctionDeclaration(
                    Api.Semantics.Visibility.Public,
                    "bar",
                    ParameterList(),
                    null,
                    BlockFunctionBody())),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(MemberExpression(NameExpression("Foo"), "bar")))))));

        var moduleDeclSyntax = tree.FindInChildren<ModuleDeclarationSyntax>(0);
        var moduleRefSyntax = tree.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        var moduleDefSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetDeclaredSymbol(moduleDeclSyntax));
        var moduleRefSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(moduleRefSyntax));

        // Assert
        Assert.True(SymbolEqualityComparer.Default.Equals(moduleDefSymbol, moduleRefSymbol));
        Assert.Empty(diags);
    }

    [Fact]
    public void ForeachSequenceHasNoGetEnumerator()
    {
        // func main() {
        //     for (i in 0) {}
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(ForExpression("i", LiteralExpression(0), BlockExpression()))))));

        // Act
        var compilation = CreateCompilation(main);

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.MemberNotFound);
    }

    [Fact]
    public void ForeachEnumeratorHasNoMoveNext()
    {
        // func foo() {
        //     for (i in Seq()) {}
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(ForExpression(
                        "i",
                        CallExpression(NameExpression("Seq")),
                        BlockExpression()))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Seq
            {
                public TestEnumerator GetEnumerator() => default;
            }

            public struct TestEnumerator
            {
                public int Current => 0;
            }
            """);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.MemberNotFound);
    }

    [Fact]
    public void ForeachEnumeratorHasNoCurrentProperty()
    {
        // func foo() {
        //     for (i in Seq()) {}
        // }

        var main = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(ForExpression(
                        "i",
                        CallExpression(NameExpression("Seq")),
                        BlockExpression()))))));

        var fooRef = CompileCSharpToMetadataRef("""
            public class Seq
            {
                public TestEnumerator GetEnumerator() => default;
            }

            public struct TestEnumerator
            {
                public int Current;

                public bool MoveNext() => true;
            }
            """);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: [main],
            metadataReferences: Basic.Reference.Assemblies.Net80.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .Append(fooRef)
                .ToImmutableArray());

        var semanticModel = compilation.GetSemanticModel(main);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.NotGettableProperty);
    }
}
