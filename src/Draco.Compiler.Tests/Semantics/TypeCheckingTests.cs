using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Types;
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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

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

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicTypes.Int32, xSym.Type);
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

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Type, IntrinsicTypes.Int32);
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

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

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

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

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
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));
        var ySym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(yDecl));

        // Assert
        Assert.Empty(diags);
        Assert.Equal(IntrinsicTypes.Int32, xSym.Type);
        Assert.Equal(IntrinsicTypes.Int32, ySym.Type);
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
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));

        // Assert
        Assert.Equal(IntrinsicTypes.Error, xSym.Type);
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.NoMatchingOverload);
    }
}
