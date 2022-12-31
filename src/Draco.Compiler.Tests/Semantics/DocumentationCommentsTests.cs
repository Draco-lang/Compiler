using Draco.Compiler.Api;
using Draco.Compiler.Api.Diagnostics;
using Draco.Compiler.Api.Syntax;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using IInternalSymbol = Draco.Compiler.Internal.Semantics.Symbols.ISymbol;
using Type = Draco.Compiler.Internal.Semantics.Types.Type;

namespace Draco.Compiler.Tests.Semantics;

public class DocumentationCommentsTests : SemanticTestsBase
{
    [Fact]
    public void FunctionDocumentationComment()
    {
        // /// This is doc comment
        // func main() {
        // }

        // Arrange
        var DocComment = "This is doc comment";
        var tree = ParseTree.Create(CompilationUnit(
            AddDocumentation(FuncDecl(
            Name("main"),
            FuncParamList(),
            null,
            BlockBodyFuncBody(BlockExpr())), "///" + DocComment)));

        var funcDecl = tree.FindInChildren<ParseNode.Decl.Func>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var funcSym = GetInternalSymbol<IInternalSymbol.IFunction>(semanticModel.GetDefinedSymbolOrNull(funcDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(funcSym.Documentation, DocComment);
    }

    [Fact]
    public void VariableDocumentationComment()
    {
        // /// This is doc comment
        // var x = 0;

        // Arrange
        var DocComment = "This is doc comment";
        var tree = ParseTree.Create(CompilationUnit(
            AddDocumentation(VariableDecl(
            Name("x"),
            null,
            LiteralExpr(0)),
            "///" + DocComment)));

        var xDecl = tree.FindInChildren<ParseNode.Decl.Variable>(0);

        // Act
        var compilation = Compilation.Create(tree);
        var semanticModel = compilation.GetSemanticModel();

        var xSym = GetInternalSymbol<IInternalSymbol.IVariable>(semanticModel.GetDefinedSymbolOrNull(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(xSym.Documentation, DocComment);
    }
}
