using System.Collections.Immutable;
using System.Text;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class DocumentationCommentsTests : SemanticTestsBase
{
    private static string CreateXmlDocComment(string originalXml)
    {
        var result = new StringBuilder();
        originalXml = originalXml.ReplaceLineEndings("\n");
        foreach (var line in originalXml.Split('\n'))
        {
            result.Append($"/// {line}{Environment.NewLine}");
        }
        return result.ToString();
    }

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
        Assert.Equal(docComment, funcSym.RawDocumentation);
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
        Assert.Equal(docComment, xSym.RawDocumentation);
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
        Assert.Equal(string.Empty, labelSym.RawDocumentation);
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
        Assert.Equal(docs, typeSym.RawDocumentation);
    }

    [Fact]
    public void NestedTypeDocumentationFromMetadata()
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

        var docs = "<summary> Documentation for NestedTestClass </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            public class TestClass
            {
                /// {{docs}}
                public class NestedTestClass { }
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<NameExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var nestedTypeSym = GetMemberSymbol<TypeSymbol>(typeSym, "NestedTestClass");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, nestedTypeSym.RawDocumentation);
    }

    [Fact]
    public void StaticTypeDocumentationFromMetadata()
    {
        // func main() {
        //   var x = TestClass.foo;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(DeclarationStatement(VariableDeclaration("x", null, MemberExpression(NameExpression("TestClass"), "foo")))))));

        var docs = "<summary> Documentation for TestClass </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            /// {{docs}}
            public static class TestClass
            {
                // Just so i can use it in draco
                public static int foo = 0;
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var @class = tree.FindInChildren<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(@class));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docs, typeSym.RawDocumentation);
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
                public void TestMethod(int arg1, string arg2) { }
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
        Assert.Equal(docs, methodSym.RawDocumentation);
    }

    [Fact]
    public void NoParamsMethodDocumentationFromMetadata()
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
            using System;

            public class TestClass
            {
                /// {{docs}}
                public void TestMethod() { }
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
        Assert.Equal(docs, methodSym.RawDocumentation);
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
        Assert.Equal(docs, fieldSym.RawDocumentation);
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
        Assert.Equal(docs, propertySym.RawDocumentation);
    }

    [Fact]
    public void GenericsDocumentationFromMetadata()
    {
        // func main() {
        //   TestClass<int32>();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(CallExpression(GenericExpression(NameExpression("TestClass"), NameType("int32"))))))));

        var classDocs = "<summary> Documentation for TestClass </summary>";
        var methodDocs = "<summary> Documentation for TestMethod </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            using System;

            /// {{classDocs}}
            public class TestClass<T>
            {
                /// {{methodDocs}}
                public void TestMethod<U>(T arg1, T arg2, U arg3) { }
            }
            """, xmlStream).DocumentationFromStream(xmlStream);

        var call = tree.FindInChildren<CallExpressionSyntax>(0);

        // Act
        var compilation = Compilation.Create(
            syntaxTrees: ImmutableArray.Create(tree),
            metadataReferences: ImmutableArray.Create(testRef));
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = GetMemberSymbol<FunctionSymbol>(typeSym, "TestMethod");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(classDocs, typeSym.GenericDefinition?.RawDocumentation);
        Assert.Equal(methodDocs, methodSym.GenericDefinition?.RawDocumentation);
    }

    [Fact]
    public void XmlDocumentationExtractorTest()
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

        var xmlDocs = """
              <summary>Documentation for TestMethod</summary>
              <param name="arg1">Documentation for arg1</param>
              <param name="arg2">Documentation for arg2</param>
              <returns>The value 0</returns>
            """;

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataRef($$"""
            public class TestClass
            {
                {{CreateXmlDocComment(xmlDocs)}}
                public int TestMethod(int arg1, string arg2) => 0; 
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

        var mdDocs = """
            # summary
            Documentation for TestMethod
            # parameters
            - [arg1](arg1): Documentation for arg1
            - [arg2](arg2): Documentation for arg2
            # returns
            The value 0
            """;

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal($"""
            <documentation>
            {xmlDocs}
            </documentation>
            """, methodSym.Documentation.ToXml().ToString(), ignoreLineEndingDifferences: true);
        Assert.Equal(mdDocs, methodSym.Documentation.ToMarkdown(), ignoreLineEndingDifferences: true);
    }
}
