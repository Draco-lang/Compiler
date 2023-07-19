using System.Collections.Immutable;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

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
        var tree = SyntaxTree.Create(CompilationUnit(
            WithDocumentation(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody()), docComment)));

        var funcDecl = tree.FindInChildren<FunctionDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var funcSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(funcDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, funcSym.Documentation);
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
        var tree = SyntaxTree.Create(CompilationUnit(
            WithDocumentation(VariableDeclaration(
            "x",
            null,
            LiteralExpression(0)),
            docComment)));

        var xDecl = tree.FindInChildren<VariableDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<GlobalSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, xSym.Documentation);
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
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(
            WithDocumentation(
                DeclarationStatement(LabelDeclaration("myLabel")),
                docComment)))));

        var labelDecl = tree.FindInChildren<LabelDeclarationSyntax>(0);

        // Act
        var compilation = Compilation.Create(ImmutableArray.Create(tree));
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetDeclaredSymbol(labelDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(string.Empty, labelSym.Documentation);
    }

    [Fact]
    public void TypeDocumentationFromMetadata()
    {
        // func main() {
        //   TestClass();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(CallExpression(NameExpression("TestClass")))))));

        var docs = "<summary> Documentation for TestClass </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            /// {{docs}}
            public class TestClass { }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, typeSym.Documentation);
    }

    [Fact]
    public void MethodDocumentationFromMetadata()
    {
        // func main() {
        //   TestClass();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(CallExpression(NameExpression("TestClass")))))));

        var docs = "<summary> Documentation for TestMethod </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            public class TestClass
            {
                /// {{docs}}
                public void TestMethod(int arg) { }
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = GetMemberSymbol<FunctionSymbol>(typeSym, "TestMethod");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, methodSym.Documentation);
    }

    [Fact]
    public void FieldDocumentationFromMetadata()
    {
        // func main() {
        //   TestClass();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(CallExpression(NameExpression("TestClass")))))));

        var docs = "<summary> Documentation for TestField </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            public class TestClass
            {
                /// {{docs}}
                public int TestField = 5;
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var fieldSym = GetMemberSymbol<FieldSymbol>(typeSym, "TestField");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, fieldSym.Documentation);
    }

    [Fact]
    public void PropertyDocumentationFromMetadata()
    {
        // func main() {
        //   TestClass();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(CallExpression(NameExpression("TestClass")))))));

        var docs = "<summary> Documentation for TestProperty </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            public class TestClass
            {
                /// {{docs}}
                public int TestProperty { get; }
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var propertySym = GetMemberSymbol<PropertySymbol>(typeSym, "TestProperty");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, propertySym.Documentation);
    }

    // TODO: Generics, Nested types
}
