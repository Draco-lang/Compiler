using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class LocalFunctionsTests : SemanticTestsBase
{
    [Fact]
    public void ParameterRedefinitionError()
    {
        // func foo() {
        //     func bar(x: int32, x: int32) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(FunctionDeclaration(
                "bar",
                ParameterList(
                    Parameter("x", NameType("int32")),
                    Parameter("x", NameType("int32"))),
                null,
                BlockFunctionBody()))))));

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
    public void UndefinedReference()
    {
        // func foo() {
        //     func bar() {
        //         var y = x;
        //     }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(FunctionDeclaration(
                "bar",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("y", null, NameExpression("x"))))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, SymbolResolutionErrors.UndefinedReference);
    }

    [Fact]
    public void LocalVariableIncompatibleType()
    {
        // func foo() {
        //     func bar() {
        //         var x: int32 = "Hello";
        //     }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(FunctionDeclaration(
                "bar",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(
                        "x",
                        NameType("int32"),
                        StringExpression("Hello"))))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, TypeCheckingErrors.TypeMismatch);
    }

    [Fact]
    public void NonUnitFunctionDoesNotReturnImplicitly()
    {
        // func foo() {
        //     func bar(): int32 {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(FunctionDeclaration(
                "bar",
                ParameterList(),
                NameType("int32"),
                BlockFunctionBody()))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, FlowAnalysisErrors.DoesNotReturn);
    }

    [Fact]
    public void ReferenceUninitializedVariable()
    {
        // func foo() {
        //     func bar() {
        //         var x: int32;
        //         var y = x;
        //     }
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "foo",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(FunctionDeclaration(
                "bar",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                    DeclarationStatement(VariableDeclaration("y", null, NameExpression("x"))))))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void LocalFunctionContributesToOverloading()
    {
        // func foo(x: int32) {}
        //
        // func main() {
        //     func foo(x: string) {}
        //
        //     foo(0);
        //     foo("Hello");
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                name: "foo",
                parameters: ParameterList(Parameter("x", NameType("int32"))),
                returnType: null,
                body: BlockFunctionBody()),
            FunctionDeclaration(
                name: "main",
                parameters: ParameterList(),
                returnType: null,
                body: BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        name: "foo",
                        parameters: ParameterList(Parameter("x", NameType("string"))),
                        returnType: null,
                        body: BlockFunctionBody())),
                    ExpressionStatement(CallExpression(NameExpression("foo"), LiteralExpression(0))),
                    ExpressionStatement(CallExpression(NameExpression("foo"), StringExpression("Hello")))))));

        var fooInt32Decl = tree.FindInChildren<FunctionDeclarationSyntax>(0);
        var fooStringDecl = tree.FindInChildren<FunctionDeclarationSyntax>(2);

        var fooInt32Call = tree.FindInChildren<CallExpressionSyntax>(0);
        var fooStringCall = tree.FindInChildren<CallExpressionSyntax>(1);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = compilation.Diagnostics;

        var fooInt32SymDecl = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooInt32Decl));
        var fooStringSymDecl = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(fooStringDecl));

        var fooInt32SymRef = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooInt32Call.Function));
        var fooStringSymRef = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(fooStringCall.Function));

        // Assert
        Assert.Empty(diagnostics);
        Assert.True(ReferenceEquals(fooInt32SymDecl, fooInt32SymRef));
        Assert.True(ReferenceEquals(fooStringSymDecl, fooStringSymRef));
    }

    // This is not done yet, we don't deal with overloads that match
#if false
    [Fact]
    public void LocalFunctionCanNotShadowOverload()
    {
        // func foo(x: int32) {}
        //
        // func main() {
        //     func foo(x: int32) {}
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            FunctionDeclaration(
                name: "foo",
                parameters: ParameterList(Parameter("x", NameType("int32"))),
                returnType: null,
                body: BlockFunctionBody()),
            FunctionDeclaration(
                name: "main",
                parameters: ParameterList(),
                returnType: null,
                body: BlockFunctionBody(
                    DeclarationStatement(FunctionDeclaration(
                        name: "foo",
                        parameters: ParameterList(Parameter("x", NameType("int32"))),
                        returnType: null,
                        body: BlockFunctionBody()))))));

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, SymbolResolutionErrors.IllegalShadowing);
    }
#endif
}
