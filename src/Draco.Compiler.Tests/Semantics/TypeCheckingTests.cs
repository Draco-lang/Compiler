using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class TypeCheckingTests : SemanticTestsBase
{
    [Fact]
    public void LocalVariableExplicitlyTyped()
    {
        // func main() {
        //     var x: int32 = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"), LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeInferredFromValue()
    {
        // func main() {
        //     var x = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", value: LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableExplicitlyTypedWithoutValue()
    {
        // func main() {
        //     var x: int32;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeInferredFromLaterAssignment()
    {
        // func main() {
        //     var x;
        //     x = 0;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0)))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void LocalVariableTypeCanNotBeInferred()
    {
        // func main() {
        //     var x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x"))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void LocalVariableIncompatibleType()
    {
        // func main() {
        //     var x: int32 = "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(
                    "x",
                    NameType("int32"),
                    StringExpression("Hello")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void LocalVariableIncompatibleTypeInferredFromUsage()
    {
        // func main() {
        //     var x;
        //     x = 0;
        //     x = "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x")),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, LiteralExpression(0))),
                ExpressionStatement(BinaryExpression(NameExpression("x"), Assign, StringExpression("Hello")))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicSymbols.Int32, xSym.Type);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void GlobalVariableExplicitlyTyped()
    {
        // var x: int32 = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", NameType("int32"), LiteralExpression(0))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableTypeInferredFromValue()
    {
        // var x = 0;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", value: LiteralExpression(0))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableExplicitlyTypedWithoutValue()
    {
        // var x: int32;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x", NameType("int32"))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicSymbols.Int32);
    }

    [Fact]
    public void GlobalVariableTypeCanNotBeInferred()
    {
        // var x;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration("x")));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void GlobalVariableIncompatibleType()
    {
        // var x: int32 = "Hello";

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            VariableDeclaration(
                "x",
                NameType("int32"),
                StringExpression("Hello"))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
        Assert.False(xSym.Type.IsError);
    }

    [Fact]
    public void BlockBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 {
        //     return "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            BlockFunctionBody(
                ExpressionStatement(ReturnExpression(StringExpression("Hello")))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void InlineBodyFunctionReturnTypeMismatch()
    {
        // func foo(): int32 = "Hello";

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            NameType("int32"),
            InlineFunctionBody(StringExpression("Hello")))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void IfConditionIsBool()
    {
        // func foo() {
        //     if (true) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(IfExpression(LiteralExpression(true), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(IfExpression(LiteralExpression(1), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void WhileConditionIsBool()
    {
        // func foo() {
        //     while (true) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(LiteralExpression(true), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(WhileExpression(LiteralExpression(1), BlockExpression()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void IfElseTypeMismatch()
    {
        // func foo() {
        //     var x = if (true) 0 else "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration(
                    "x",
                    value: IfExpression(
                        condition: LiteralExpression(true),
                        then: LiteralExpression(0),
                        @else: StringExpression("Hello"))))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.TypeMismatch);
    }

    // TODO: Unspecified if we want this
    // There is a strong case against this, since with unit being implicit return type,
    // it's not intuitive to read
#if false
    [Fact]
    public void AllowNonUnitExpressionInInlineFuncReturningUnit()
    {
        // func foo(): int32 = 0;
        // func bar() = foo();

        // Arrange
        var tree = CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                NameType("int32"),
                InlineFunctionBody(LiteralExpression(0))),
            FunctionDeclaration(
                bar",
                ParameterList(),
                null,
                InlineFunctionBody(CallExpr(NameExpression("foo")))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);
        var barDecl = tree.FindInChildren<ParseNode.Decl.Func>(1);
        var fooSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbol(fooDecl));
        var barSymbol = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbol(barDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(Internal.Types.Intrinsics.Int32, fooSymbol.ReturnType);
        Assert.Equal(Type.Unit, barSymbol.ReturnType);
    }
#endif

    [Fact]
    public void NeverTypeCompatibility()
    {
        // func foo() {
        // start:
        //     var x = if (true) 0 else return;
        //     var y = if (true) 0 else goto start;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(LabelDeclaration("start")),
                    DeclarationStatement(VariableDeclaration("x", value: IfExpression(LiteralExpression(true), LiteralExpression(0), ReturnExpression()))),
                    DeclarationStatement(VariableDeclaration("y", value: IfExpression(LiteralExpression(true), LiteralExpression(0), GotoExpression("start"))))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var yDecl = tree.FindInChildren<VariableDeclarationSyntax>(1);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var ySym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(yDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(IntrinsicSymbols.Int32, xSym.Type);
        Assert.Equal(IntrinsicSymbols.Int32, ySym.Type);
    }

    [Fact]
    public void NoOverloadForOperator()
    {
        // func foo() {
        //     var x = 1 + "Hello";
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "x",
                        value: BinaryExpression(LiteralExpression(1), Plus, StringExpression("Hello"))))))));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicSymbols.ErrorType, xSym.Type);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }

    [Fact]
    public void OkOverloading()
    {
        // func foo(x: int32) { }
        // func foo(x: bool) { }
        //
        // func main() {
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("bool"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.Same(fooBoolDeclSym, fooBoolRefSym);
    }

    [Fact]
    public void OkNestedOverloading()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        //
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody())),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.Same(fooBoolDeclSym, fooBoolRefSym);
    }

    [Fact]
    public void NestedOverloadNotVisibleFromOutside()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        // }
        //
        // func main() {
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody())))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooInt32RefSyntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolDeclSyntax));
        var fooInt32RefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32RefSyntax));
        var fooBoolRefSym = semanticModel.GetDeclaredSymbol(fooBoolRefSyntax);

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
        Assert.NotSame(fooInt32DeclSym, fooBoolDeclSym);
        Assert.Same(fooInt32DeclSym, fooInt32RefSym);
        Assert.NotSame(fooBoolDeclSym, fooBoolRefSym);
        Assert.NotNull(fooBoolRefSym);
        Assert.True(fooBoolRefSym.IsError);
    }

    [Fact]
    public void IllegalOverloading()
    {
        // func foo(x: int32) { }
        // func foo(x: int32) { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody()),
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody())));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Equal(2, diags.Length);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalOverloadDefinition);
    }

    [Fact]
    public void IllegalNestedOverloading()
    {
        // func foo(x: int32) {
        //     func foo(x: int32) { }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("int32"))),
                        null,
                        BlockFunctionBody()))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.IllegalOverloadDefinition);
    }

    [Fact]
    public void NestedMatchingLocalOverloads()
    {
        // func foo(x: int32) {
        //     func foo(x: bool) { }
        //
        //     foo(0);
        //     foo(true);
        // }
        //
        // func main() {
        //     func foo(x: bool) {}
        //
        //     foo(0);
        //     foo(true);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody(
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true)))))))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        "foo",
                        ParameterList(Parameter("x", NameType("bool"))),
                        null,
                        BlockFunctionBody(
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                            ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(true))))))))));

        var fooInt32DeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooBoolInFooDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(1);
        var fooBoolInMainDeclSyntax = tree.FindInChildren<FunctionDeclarationSyntax>(3);
        var fooInt32Ref1Syntax = tree.FindInChildren<CallExpressionSyntax>(0).Function;
        var fooBoolInFooRefSyntax = tree.FindInChildren<CallExpressionSyntax>(1).Function;
        var fooInt32Ref2Syntax = tree.FindInChildren<CallExpressionSyntax>(2).Function;
        var fooBoolInMainRefSyntax = tree.FindInChildren<CallExpressionSyntax>(3).Function;

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var fooInt32DeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32DeclSyntax));
        var fooBoolInFooDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInFooDeclSyntax));
        var fooBoolInMainDeclSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInMainDeclSyntax));
        var fooInt32Ref1Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32Ref1Syntax));
        var fooInt32Ref2Sym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32Ref2Syntax));
        var fooBoolInFooRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInFooRefSyntax));
        var fooBoolInMainRefSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooBoolInMainRefSyntax));

        // Assert
        Assert.Empty(diags);
        Assert.Same(fooInt32DeclSym, fooInt32Ref1Sym);
        Assert.Same(fooInt32DeclSym, fooInt32Ref2Sym);
        Assert.Same(fooBoolInFooDeclSym, fooBoolInFooRefSym);
        Assert.Same(fooBoolInMainDeclSym, fooBoolInMainRefSym);
        Assert.NotSame(fooBoolInFooDeclSym, fooBoolInMainDeclSym);
    }

    [Fact]
    public void IllegalCallToNonFunctionType()
    {
        // func foo() {
        //     var a = 0;
        //     a();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("a", null, LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("a")))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CallNonFunction);
    }

    [Fact]
    public void ExplicitGenericFunction()
    {
        // func identity<T>(x: T): T = x;
        //
        // func main() {
        //     var a = identity<int32>(1);
        //     var b = identity<string>("foo");
        //     var c = identity<int32>("foo");
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "a",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32")),
                            LiteralExpression(0)))),
                    DeclarationStatement(VariableDeclaration(
                        "b",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("string")),
                            StringExpression("foo")))),
                    DeclarationStatement(VariableDeclaration(
                        "c",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32")),
                            StringExpression("foo"))))))));

        var identitySyntax = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var firstCallSyntax = tree.FindInChildren<CallExpressionSyntax>(0);

        var aSyntax = tree.FindInChildren<VariableDeclarationSyntax>(0);
        var bSyntax = tree.FindInChildren<VariableDeclarationSyntax>(1);
        var cSyntax = tree.FindInChildren<VariableDeclarationSyntax>(2);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var identitySym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(identitySyntax));
        var firstCalledSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(firstCallSyntax.Function));

        var aSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(aSyntax));
        var bSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(bSyntax));
        var cSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(cSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        // NOTE: This might not be the best error...
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);

        Assert.True(identitySym.IsGenericDefinition);
        Assert.True(firstCalledSym.IsGenericInstance);
        Assert.False(firstCalledSym.IsGenericDefinition);
        Assert.Same(identitySym, firstCalledSym.GenericDefinition);
        Assert.Single(firstCalledSym.GenericArguments);
        Assert.Same(IntrinsicSymbols.Int32, firstCalledSym.GenericArguments[0]);

        Assert.Same(IntrinsicSymbols.Int32, aSym.Type);
        Assert.Same(IntrinsicSymbols.String, bSym.Type);
        Assert.True(cSym.Type.IsError);
    }

    [Fact]
    public void ExplicitGenericFunctionWithWrongNumberOfArgs()
    {
        // func identity<T>(x: T): T = x;
        //
        // func main() {
        //     var a = identity<int32, int32>(1);
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                "identity",
                GenericParameterList(GenericParameter("T")),
                ParameterList(Parameter("x", NameType("T"))),
                NameType("T"),
                InlineFunctionBody(NameExpression("x"))),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "a",
                        null,
                        CallExpression(
                            GenericExpression(NameExpression("identity"), NameType("int32"), NameType("int32")),
                            LiteralExpression(0))))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoGenericFunctionWithParamCount);
    }

    [Fact]
    public void ExplicitGenericTypeWithWrongNumberOfArgs()
    {
        // import System.Collections.Generic;
        // var l: List<int32, int32>;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic"),
            VariableDeclaration(
                "l",
                GenericType(NameType("List"), NameType("int32"), NameType("int32")))));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.GenericTypeParamCountMismatch);
    }
}
