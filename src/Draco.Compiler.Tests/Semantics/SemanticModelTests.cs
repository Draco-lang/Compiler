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

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: Basic.Reference.Assemblies.Net70.ReferenceInfos.All
                .Select(r => MetadataReference.FromPeStream(new MemoryStream(r.ImageBytes)))
                .ToImmutableArray());
        var semanticModel = compilation.GetSemanticModel(tree);

        var writeLineSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(memberExprSyntax));
        var consoleSymbol = GetInternalSymbol<TypeSymbol>(semanticModel.GetReferencedSymbol(consoleSyntax));

        var diags = semanticModel.Diagnostics;

        // Assert
        Assert.Empty(diags);
        Assert.NotNull(writeLineSymbol);
        Assert.NotNull(consoleSymbol);
        Assert.Contains(writeLineSymbol, consoleSymbol.Members);
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
}
