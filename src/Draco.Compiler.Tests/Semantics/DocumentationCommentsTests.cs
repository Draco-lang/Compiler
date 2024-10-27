using System.Text;
using System.Xml;
using System.Xml.Linq;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class DocumentationCommentsTests
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

    private static string AddDocumentationTag(string insideXml) => $"""
            <documentation>
            {insideXml}
            </documentation>
            """;

    private static string PrettyXml(XElement element)
    {
        var stringBuilder = new StringBuilder();

        var settings = new XmlWriterSettings()
        {
            OmitXmlDeclaration = true,
            Indent = true,
            IndentChars = string.Empty,
            NewLineOnAttributes = false,
        };

        using (var xmlWriter = XmlWriter.Create(stringBuilder, settings))
        {
            element.Save(xmlWriter);
        }

        return stringBuilder.ToString();
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

        var funcDecl = tree.GetNode<FunctionDeclarationSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var funcSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(funcDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, funcSym.Documentation.ToMarkdown());
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
            true,
            "x",
            null,
            LiteralExpression(0)),
            docComment)));

        var xDecl = tree.GetNode<VariableDeclarationSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var xSym = GetInternalSymbol<FieldSymbol>(semanticModel.GetDeclaredSymbol(xDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, xSym.Documentation.ToMarkdown());
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

        var labelDecl = tree.GetNode<LabelDeclarationSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var labelSym = GetInternalSymbol<LabelSymbol>(semanticModel.GetDeclaredSymbol(labelDecl));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(string.Empty, labelSym.Documentation.ToMarkdown());
    }

    [Theory]
    [InlineData("This is doc comment")]
    [InlineData("""
        This is
        multiline doc comment
        """)]
    public void ModuleDocumentationComment(string docComment)
    {
        // /// This is doc comment
        // module documentedModule{
        //     public var Foo = 0;
        // }
        //
        // func foo(){
        //     var x = documentedModule.Foo;
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            WithDocumentation(ModuleDeclaration(
            "documentedModule",
            VariableDeclaration(Api.Semantics.Visibility.Public, true, "Foo", null, LiteralExpression(0))),
            docComment),
            FunctionDeclaration(
                "foo",
                ParameterList(),
                null,
                BlockFunctionBody(
                    DeclarationStatement(VariableDeclaration(true, "x", null, MemberExpression(NameExpression("documentedModule"), "Foo")))))));

        var moduleRef = tree.GetNode<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var moduleSym = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(moduleRef));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(docComment, moduleSym.Documentation.ToMarkdown());
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

        var testRef = CompileCSharpToMetadataReference($$"""
            /// {{docs}}
            public class TestClass { }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(typeSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            public class TestClass
            {
                /// {{docs}}
                public class NestedTestClass { }
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var nestedTypeSym = AssertMember<TypeSymbol>(typeSym, "NestedTestClass");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(nestedTypeSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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
            BlockFunctionBody(DeclarationStatement(VariableDeclaration(true, "x", null, MemberExpression(NameExpression("TestClass"), "foo")))))));

        var docs = "<summary> Documentation for TestClass </summary>";

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataReference($$"""
            /// {{docs}}
            public static class TestClass
            {
                // Just so i can use it in draco
                public static int foo = 0;
            }
            """, xmlDocStream: xmlStream);

        var @class = tree.GetNode<MemberExpressionSyntax>(0).Accessed;

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<ModuleSymbol>(semanticModel.GetReferencedSymbol(@class));

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(typeSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            public class TestClass
            {
                /// {{docs}}
                public void TestMethod(int arg1, string arg2) { }
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = AssertMember<FunctionSymbol>(typeSym, "TestMethod");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(methodSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            using System;

            public class TestClass
            {
                /// {{docs}}
                public void TestMethod() { }
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = AssertMember<FunctionSymbol>(typeSym, "TestMethod");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(methodSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            public class TestClass
            {
                /// {{docs}}
                public int TestField = 5;
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var fieldSym = AssertMember<FieldSymbol>(typeSym, "TestField");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(fieldSym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            public class TestClass
            {
                /// {{docs}}
                public int TestProperty { get; }
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var propertySym = AssertMember<PropertySymbol>(typeSym, "TestProperty");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(docs), PrettyXml(propertySym.Documentation.ToXml()), ignoreLineEndingDifferences: true);
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

        var testRef = CompileCSharpToMetadataReference($$"""
            using System;

            /// {{classDocs}}
            public class TestClass<T>
            {
                /// {{methodDocs}}
                public void TestMethod<U>(T arg1, T arg2, U arg3) { }
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<CallExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = AssertMember<FunctionSymbol>(typeSym, "TestMethod");

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(classDocs), PrettyXml(typeSym.GenericDefinition!.Documentation.ToXml()), ignoreLineEndingDifferences: true);
        Assert.Equal(AddDocumentationTag(methodDocs), PrettyXml(methodSym.GenericDefinition!.Documentation.ToXml()), ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void XmlDocumentationExtractorTest()
    {
        // import TestNamespace;
        // func main() {
        //   TestClass();
        // }

        // Arrange
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("TestNamespace"),
            FunctionDeclaration(
                "main",
                ParameterList(),
                null,
                BlockFunctionBody(ExpressionStatement(CallExpression(NameExpression("TestClass")))))));

        var originalDocs = """
            <summary>Documentation for TestMethod, which is in <see cref="TestClass" />, random generic link <see cref="System.Collections.Generic.List{int}" /></summary>
            <param name="arg1">Documentation for arg1</param>
            <param name="arg2">Documentation for arg2</param>
            <typeparam name="T">Useless type param</typeparam>
            <code>
            var x = 0;
            void Foo(int z) { }
            </code>
            <returns><paramref name="arg1" /> added to <paramref name="arg2" />, <typeparamref name="T" /> is not used</returns>
            """;

        var xmlStream = new MemoryStream();

        var testRef = CompileCSharpToMetadataReference($$"""
            namespace TestNamespace;
            public class TestClass
            {
                {{CreateXmlDocComment(originalDocs)}}
                public int TestMethod<T>(int arg1, int arg2) => arg1 + arg2;
            }
            """, xmlDocStream: xmlStream);

        var call = tree.GetNode<NameExpressionSyntax>(0);

        // Act
        var compilation = CreateCompilation(
            syntaxTrees: [tree],
            additionalReferences: [testRef]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var typeSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(call)).ReturnType;
        var methodSym = AssertMember<FunctionSymbol>(typeSym, "TestMethod");

        var xmlGeneratedDocs = """
            <summary>Documentation for TestMethod, which is in <see cref="T:TestNamespace.TestClass" />, random generic link <see cref="T:System.Collections.Generic.List`1" /></summary>
            <param name="arg1">Documentation for arg1</param>
            <param name="arg2">Documentation for arg2</param>
            <typeparam name="T">Useless type param</typeparam>
            <code>var x = 0;
            void Foo(int z) { }</code>
            <returns>
            <paramref name="arg1" /> added to <paramref name="arg2" />, <typeparamref name="T" /> is not used</returns>
            """;

        var mdGeneratedDocs = """
            Documentation for TestMethod, which is in [TestNamespace.TestClass](), random generic link [System.Collections.Generic.List<T>]()
            # parameters
            - arg1: Documentation for arg1
            - arg2: Documentation for arg2
            # type parameters
            - T: Useless type param
            ```cs
            var x = 0;
            void Foo(int z) { }
            ```
            # returns
            [arg1]() added to [arg2](), [T]() is not used
            """;

        var resultXml = PrettyXml(methodSym.Documentation.ToXml());
        var resultMd = methodSym.Documentation.ToMarkdown();

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(AddDocumentationTag(xmlGeneratedDocs), resultXml, ignoreLineEndingDifferences: true);
        Assert.Equal(mdGeneratedDocs, resultMd, ignoreLineEndingDifferences: true);
    }

    [Fact]
    public void MarkdownDocumentationExtractorTest()
    {
        // Arrange
        var originalDocs = """
            Documentation for TestMethod, which is in [TestNamespace.TestClass](), random generic link [System.Collections.Generic.List<T>]()
            # parameters
            - arg1: Documentation for arg1
            - arg2: Documentation for arg2
            # type parameters
            - T: Useless type param
            ```cs
            var x = 0;
            void Foo(int z) { }
            ```
            # returns
            [arg1]() added to [arg2](), [T]() is not used
            """;

        // /// documentation
        // func TestMethod() { }

        var tree = SyntaxTree.Create(CompilationUnit(
            WithDocumentation(FunctionDeclaration(
                "TestMethod",
                ParameterList(),
                null,
                BlockFunctionBody()), originalDocs)));

        var testMethodDecl = tree.GetNode<FunctionDeclarationSyntax>(0);

        // Act
        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var methodSym = GetInternalSymbol<FunctionSymbol>(semanticModel.GetDeclaredSymbol(testMethodDecl));

        var resultMd = methodSym.Documentation.ToMarkdown();

        // Assert
        Assert.Empty(semanticModel.Diagnostics);
        Assert.Equal(originalDocs, resultMd);
        Assert.Equal(methodSym.Documentation.ToMarkdown(), resultMd);
    }
}
