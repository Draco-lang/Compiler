using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Binding;
using Draco.Compiler.Internal.FlowAnalysis;
using Draco.Compiler.Internal.Symbols;
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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));
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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));
        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));
        _ = GetInternalSymbol<LocalSymbol>(semanticModel.GetDefinedSymbol(xDecl));
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
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        _ = GetInternalSymbol<SourceFunctionSymbol>(semanticModel.GetDefinedSymbol(mainDecl));
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
        var writeLineSyntax = tree.PreOrderTraverse().OfType<SyntaxToken>().First(t => t.Text == "WriteLine");

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var writeLineFromMemberSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var consoleSymbol = GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(consoleSyntax));
        var writeLineFromNameSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(writeLineSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(writeLineFromMemberSymbol);
        Assert.NotNull(consoleSymbol);
        Assert.Contains(writeLineFromMemberSymbol, consoleSymbol.Members);
        Assert.Same(writeLineFromMemberSymbol, writeLineFromNameSymbol);
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
        var consoleSyntax = tree.PreOrderTraverse().OfType<SyntaxToken>().First(t => t.Text == "Console");
        var writeLineSyntax = tree.PreOrderTraverse().OfType<SyntaxToken>().First(t => t.Text == "WriteLine");

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var writeLineFromMemberSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var consoleFromMemberSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(memberSubexprSyntax));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));
        var consoleSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(consoleSyntax));
        var writeLineSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(writeLineSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(writeLineFromMemberSymbol);
        Assert.NotNull(consoleFromMemberSymbol);
        Assert.NotNull(systemSymbol);
        Assert.Contains(writeLineFromMemberSymbol, consoleSymbol.Members);
        Assert.Contains(consoleSymbol, systemSymbol.Members);
        Assert.Same(writeLineFromMemberSymbol, writeLineSymbol);
        Assert.Same(consoleFromMemberSymbol, consoleSymbol);
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
        var consoleSyntax = tree.PreOrderTraverse().OfType<SyntaxToken>().First(t => t.Text == "Console");
        var missingNameSyntax = tree
            .PreOrderTraverse()
            .OfType<SyntaxToken>()
            .First(t => t.Kind == TokenKind.Identifier && string.IsNullOrWhiteSpace(t.Text));

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var errorFromMemberSymbol = semanticModel.GetReferencedSymbol(memberExprSyntax);
        var consoleFromMemberSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(memberSubexprSyntax));
        var systemSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(systemSyntax));
        var consoleSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(consoleSyntax));
        var errorSymbol = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(missingNameSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Single(diags);
        Assert.NotNull(errorFromMemberSymbol);
        Assert.NotNull(consoleFromMemberSymbol);
        Assert.NotNull(systemSymbol);
        Assert.Contains(consoleFromMemberSymbol, systemSymbol.Members);
        Assert.Same(consoleFromMemberSymbol, consoleSymbol);
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

        // TODO: We need a way to access 'AppendLine' and test for what it references

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
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
    public void GetPathSymbolsFromImport()
    {
        // import System.Collections.Generic;
        // func main() { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Generic"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody())));

        // Act

        // Assert

        // TODO
        Assert.Fail("We need import elements to actually have some differentiating syntax");
    }

    [Fact]
    public void GetPathSymbolsFromPartiallyNonExistingImport()
    {
        // import System.Collections.Nonexisting.Foo;
        // func main() { }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System", "Collections", "Nonexisting", "Foo"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody())));

        // Act

        // Assert

        // TODO
        Assert.Fail("We need import elements to actually have some differentiating syntax");
    }
}
