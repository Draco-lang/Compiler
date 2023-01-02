using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;

namespace Draco.Compiler.Tests.Semantics;

public sealed class DocumentationCommentsTests : SemanticTestsBase
{
    [Theory]
    [InlineData("This is doc comment")]
    [InlineData("""
        This is
        multiline doc comment
        """)]
    public void FunctionDocumentationComment(string docComment)
    {
        // /// This is doc comment
        // func main() {
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(
            WithDocumentation(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr())), docComment)));

        var funcDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var funcSym = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(funcDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, funcSym.Documentation, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("This is doc comment")]
    [InlineData("""
        This is
        multiline doc comment
        """)]
    public void VariableDocumentationComment(string docComment)
    {
        // /// This is doc comment
        // var x = 0;

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(
            WithDocumentation(VariableDecl(
            Name("x"),
            null,
            LiteralExpr(0)),
            docComment)));

        var xDecl = tree.FindInChildren<ParseNode.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, xSym.Documentation, ignoreLineEndingDifferences: true);
    }

    [Theory]
    [InlineData("This is doc comment")]
    [InlineData("""
        This is
        multiline doc comment
        """)]
    public void LabelDocumentationComment(string docComment)
    {
        // func main() {
        //     /// This is doc comment
        //     myLabel:        
        // }

        // Arrange
        var tree = ParseTree.Create(CompilationUnit(
            FuncDecl(Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr(
            WithDocumentation(DeclStmt(LabelDecl("myLabel")),
            docComment))))));

        var labelDecl = tree.FindInChildren<ParseNode.Decl.Label>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var labelSym = GetInternalSymbol<IInternalSymbol.ILabel>(semanticModel.GetDefinedSymbolOrNull(labelDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(string.Empty, labelSym.Documentation, ignoreLineEndingDifferences: true);
    }
}
