using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
using Draco.Compiler.Internal.Symbols.Error;
using Draco.Compiler.Internal.Symbols.Source;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;

namespace Draco.Compiler.Tests.Semantics;

public sealed class SemanticModelTests : SemanticTestsBase
{
    // Reported in https://github.com/Draco-lang/Compiler/issues/220
    [Fact]
    public void RequestingDiagnosticsAfterSymbolDoesNotDiscardDiagnostic()
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void RequestingSymbolAfterDiagnosticsDoesNotDiscardDiagnostic()
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diags = semanticModel.Diagnostics;
        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
        Assert.True(xSym.Type.IsError);
    }

    [Fact]
    public void RequestingSymbolMultipleTimesDoesNotMultiplyDiagnostic()
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
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));
        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, TypeCheckingErrors.CouldNotInferType);
    }

    [Fact]
    public void RequestingFunctionBodyDoesNotDiscardFlowAnalysisDiagnostic()
    {
        // func main() {
        //     var x: int32;
        //     var y = x;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(VariableDeclaration("x", NameType("int32"))),
                DeclarationStatement(VariableDeclaration("y", value: NameExpression("x")))))));

        var mainDecl = tree.FindInChildren<FunctionDeclarationSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        _ = GetInternalSymbol<SourceFunctionSymbol>(semanticModel.GetDeclaredSymbol(mainDecl));
        _ = mainDecl.Body;

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, FlowAnalysisErrors.VariableUsedBeforeInit);
    }

    [Fact]
    public void GetReferencedSymbolFromModuleMemberAccess()
    {
        // func main() {
        //     import System;
        //     Console.WriteLine();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System")),
                ExpressionStatement(CallExpression(MemberExpression(NameExpression("Console"), "WriteLine")))))));

        var memberExprSyntax = tree.FindInChildren<MemberExpressionSyntax>(0);
        var consoleSyntax = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var writeLineSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var consoleSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(consoleSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(writeLineSymbol);
        Assert.NotNull(consoleSymbol);
        Assert.Contains(writeLineSymbol, consoleSymbol.Members);
    }

    [Fact]
    public void GetReferencedSymbolFromFullyQualifiedName()
    {
        // func main() {
        //     System.Console.WriteLine();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(CallExpression(
                    MemberExpression(
                        MemberExpression(NameExpression("System"), "Console"),
                        "WriteLine")))))));

        var memberExprSyntax = tree.FindInChildren<MemberExpressionSyntax>(0);
        var memberSubexprSyntax = tree.FindInChildren<MemberExpressionSyntax>(1);
        var systemSyntax = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var writeLineSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var consoleSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(memberSubexprSyntax));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(writeLineSymbol);
        Assert.NotNull(consoleSymbol);
        Assert.NotNull(systemSymbol);
        Assert.Contains(writeLineSymbol, consoleSymbol.Members);
        Assert.Contains(consoleSymbol, systemSymbol.Members);
    }

    [Fact]
    public void GetReferencedSymbolFromFullyQualifiedType()
    {
        // func make(): System.Text.StringBuilder = System.Text.StringBuilder();

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "make",
            ParameterList(),
            MemberType(
                MemberType(NameType("System"), "Text"),
                "StringBuilder"),
            InlineFunctionBody(
                CallExpression(
                    MemberExpression(
                        MemberExpression(NameExpression("System"), "Text"),
                        "StringBuilder"))))));

        var memberTypeSyntax = tree.FindInChildren<MemberTypeSyntax>(0);
        var memberSubtypeSyntax = tree.FindInChildren<MemberTypeSyntax>(1);
        var systemSyntax = tree.FindInChildren<NameTypeSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var sbSymbol = GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(memberTypeSyntax));
        var textSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(memberSubtypeSyntax));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(sbSymbol);
        Assert.NotNull(textSymbol);
        Assert.NotNull(systemSymbol);
        Assert.Contains(sbSymbol, textSymbol.Members);
        Assert.Contains(textSymbol, systemSymbol.Members);
    }

    [Fact]
    public void GetReferencedSymbolFromFullyQualifiedIncompleteName()
    {
        // func main() {
        //     System.Console.;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                ExpressionStatement(CallExpression(
                    MemberExpression(
                        MemberExpression(NameExpression("System"), "Console"),
                        Dot,
                        Missing(TokenKind.Identifier))))))));

        var memberExprSyntax = tree.FindInChildren<MemberExpressionSyntax>(0);
        var memberSubexprSyntax = tree.FindInChildren<MemberExpressionSyntax>(1);
        var systemSyntax = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var errorSymbol = semanticModel.GetReferencedSymbol(memberExprSyntax);
        var consoleSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(memberSubexprSyntax));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.NotNull(errorSymbol);
        Assert.NotNull(consoleSymbol);
        Assert.NotNull(systemSymbol);
        Assert.Contains(consoleSymbol, systemSymbol.Members);
        Assert.True(errorSymbol.IsError);
    }

    [Fact]
    public void GetReferencedSymbolFromTypeMemberAccess()
    {
        // func main() {
        //     import System.Text;
        //     var builder = StringBuilder();
        //     builder.AppendLine();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System", "Text")),
                DeclarationStatement(VariableDeclaration("builder", null, CallExpression(NameExpression("StringBuilder")))),
                ExpressionStatement(CallExpression(MemberExpression(NameExpression("builder"), "AppendLine")))))));

        var memberExprSyntax = tree.FindInChildren<MemberExpressionSyntax>(0);
        var builderNameSyntax = tree.FindInChildren<NameExpressionSyntax>(1);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var appendLineSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var builderSymbol = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(builderNameSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(appendLineSymbol);
        Assert.NotNull(builderSymbol);
        Assert.Contains(appendLineSymbol, builderSymbol.Type.Members);
    }

    [Fact]
    public void GetReferencedSymbolFromTypeMemberAccessWithNonExistingMember()
    {
        // func main() {
        //     import System.Text;
        //     var builder = StringBuilder();
        //     builder.Ap();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System", "Text")),
                DeclarationStatement(VariableDeclaration("builder", null, CallExpression(NameExpression("StringBuilder")))),
                ExpressionStatement(CallExpression(MemberExpression(NameExpression("builder"), "Ap")))))));

        var memberExprSyntax = tree.FindInChildren<MemberExpressionSyntax>(0);
        var builderNameSyntax = tree.FindInChildren<NameExpressionSyntax>(1);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var appendLineSymbol = GetInternalSymbol<UndefinedMemberSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var builderSymbol = GetInternalSymbol<LocalSymbol>(semanticModel.GetReferencedSymbol(builderNameSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        AssertDiagnostic(diags, SymbolResolutionErrors.MemberNotFound);
        Assert.NotNull(appendLineSymbol);
        Assert.NotNull(builderSymbol);
        Assert.False(builderSymbol.IsError);
        Assert.True(appendLineSymbol.IsError);
    }

    [Fact]
    public void GetPathSymbolsFromImport()
    {
        // import System.Collections.Generic;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic")));

        var systemSyntax = tree.FindInChildren<RootImportPathSyntax>(0);
        var systemCollectionsSyntax = tree.FindInChildren<MemberImportPathSyntax>(1);
        var systemCollectionsGenericSyntax = tree.FindInChildren<MemberImportPathSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));
        var systemCollectionsSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemCollectionsSyntax));
        var systemCollectionsGenericSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemCollectionsGenericSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(systemSymbol);
        Assert.NotNull(systemCollectionsSymbol);
        Assert.NotNull(systemCollectionsGenericSymbol);
        Assert.Contains(systemCollectionsSymbol, systemSymbol.Members);
        Assert.Contains(systemCollectionsGenericSymbol, systemCollectionsSymbol.Members);
    }

    [Fact]
    public void GetPathSymbolsFromLocalImport()
    {
        // func main() {
        //     import System.Collections.Generic;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
                DeclarationStatement(ImportDeclaration("System", "Collections", "Generic"))))));

        var systemSyntax = tree.FindInChildren<RootImportPathSyntax>(0);
        var systemCollectionsSyntax = tree.FindInChildren<MemberImportPathSyntax>(1);
        var systemCollectionsGenericSyntax = tree.FindInChildren<MemberImportPathSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));
        var systemCollectionsSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemCollectionsSyntax));
        var systemCollectionsGenericSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemCollectionsGenericSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(systemSymbol);
        Assert.NotNull(systemCollectionsSymbol);
        Assert.NotNull(systemCollectionsGenericSymbol);
        Assert.Contains(systemCollectionsSymbol, systemSymbol.Members);
        Assert.Contains(systemCollectionsGenericSymbol, systemCollectionsSymbol.Members);
    }

    [Fact]
    public void GetPathSymbolsFromPartiallyNonExistingImport()
    {
        // import System.Collections.Nonexisting.Foo;

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Nonexisting", "Foo")));

        var systemSyntax = tree.FindInChildren<RootImportPathSyntax>(0);
        var collectionsSyntax = tree.FindInChildren<MemberImportPathSyntax>(2);
        var nonexistingSyntax = tree.FindInChildren<MemberImportPathSyntax>(1);
        var fooSyntax = tree.FindInChildren<MemberImportPathSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));
        var collectionsSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(collectionsSyntax));
        var nonexistingSymbol = GetInternalSymbol<UndefinedMemberSymbol>(semanticModel.GetReferencedSymbol(nonexistingSyntax));
        var fooSymbol = GetInternalSymbol<UndefinedMemberSymbol>(semanticModel.GetReferencedSymbol(fooSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.NotNull(systemSymbol);
        Assert.NotNull(collectionsSymbol);
        Assert.NotNull(nonexistingSymbol);
        Assert.NotNull(fooSymbol);
        Assert.Contains(collectionsSymbol, systemSymbol.Members);
        Assert.True(nonexistingSymbol.IsError);
        Assert.True(fooSymbol.IsError);
    }
}
