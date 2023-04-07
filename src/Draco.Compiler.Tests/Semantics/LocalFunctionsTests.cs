using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Binding;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.FlowAnalysis;

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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        var x1SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDefinedSymbol(x1Decl));
        var x2SymDecl = GetInternalSymbol<ParameterSymbol>(semanticModel.GetDefinedSymbol(x2Decl));

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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
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
        //     func bar(): int32 {
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
                NameType("int32"),
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                    DeclarationStatement(VariableDeclaration("y", null, NameExpression("x"))))))))));

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);
        var diagnostics = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diagnostics);
        AssertDiagnostic(diagnostics, FlowAnalysisErrors.VariableUsedBeforeInit);
    }
}
