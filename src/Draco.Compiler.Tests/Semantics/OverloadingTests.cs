using System.Diagnostics;
using Draco.Compiler.Api;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class OverloadingTests
{
    // func foo(l: List<int32>)         {}
    // func foo(l: List<string>)        {}
    // func foo<T>(l: List<T>)          {}
    // func foo(l: IEnumerable<int32>)  {}
    // func foo(l: IEnumerable<string>) {}
    // func foo<T>(l: IEnumerable<T>)   {}
    private static IEnumerable<DeclarationSyntax> GetGenericListOverloads() => [
        FunctionDeclaration(
            "foo",
            ParameterList(NormalParameter("l", GenericType(NameType("List"), NameType("int32")))),
            null,
            BlockFunctionBody()),
        FunctionDeclaration(
            "foo",
            ParameterList(NormalParameter("l", GenericType(NameType("List"), NameType("string")))),
            null,
            BlockFunctionBody()),
        FunctionDeclaration(
            "foo",
            GenericParameterList(GenericParameter("T")),
            ParameterList(NormalParameter("l", GenericType(NameType("List"), NameType("T")))),
            null,
            BlockFunctionBody()),
        FunctionDeclaration(
            "foo",
            ParameterList(NormalParameter("l", GenericType(NameType("IEnumerable"), NameType("int32")))),
            null,
            BlockFunctionBody()),
        FunctionDeclaration(
            "foo",
            ParameterList(NormalParameter("l", GenericType(NameType("IEnumerable"), NameType("string")))),
            null,
            BlockFunctionBody()),
        FunctionDeclaration(
            "foo",
            GenericParameterList(GenericParameter("T")),
            ParameterList(NormalParameter("l", GenericType(NameType("IEnumerable"), NameType("T")))),
            null,
            BlockFunctionBody()),
    ];

    // import System.Collections.Generic;
    // import System.Linq.Enumerable;
    //
    // ... foo overloads ...
    //
    // func main() {
    //     call();
    //
    private static SyntaxTree CreateOverloadTree(CallExpressionSyntax call) => SyntaxTree.Create(CompilationUnit(
        new DeclarationSyntax[]
        {
            ImportDeclaration("System", "Collections", "Generic"),
            ImportDeclaration("System", "Linq", "Enumerable"),
        }.Concat(GetGenericListOverloads())
        .Append(FunctionDeclaration(
            "main",
            ParameterList(),
            null,
            BlockFunctionBody(ExpressionStatement(call))))));

    private static FunctionSymbol GetDeclaredFunctionSymbol(Compilation compilation, int index)
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var syntax = syntaxTree.GetNode<FunctionDeclarationSyntax>(index);
        Debug.Assert(syntax is not null);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(syntax);
        Debug.Assert(symbol!.Name == "foo");

        return GetInternalSymbol<FunctionSymbol>(symbol);
    }

    private static FunctionSymbol GetCalledFunctionSymbol(Compilation compilation)
    {
        var syntaxTree = compilation.SyntaxTrees.Single();
        var syntax = syntaxTree.GetNode<CallExpressionSyntax>();
        Debug.Assert(syntax is not null);

        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var symbol = semanticModel.GetReferencedSymbol(syntax);
        Debug.Assert(symbol!.Name == "foo");

        return GetInternalSymbol<FunctionSymbol>(symbol);
    }

    [Fact]
    public void ListInt32Overload()
    {
        // foo(List<int32>());

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(GenericExpression(NameExpression("List"), NameType("int32")))));

        // Act
        var compilation = CreateCompilation(tree);
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 0);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.Same(expectedSymbol, calledSymbol);
    }

    [Fact]
    public void ListStringOverload()
    {
        // foo(List<string>());

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(GenericExpression(NameExpression("List"), NameType("string")))));
        var compilation = CreateCompilation(tree);

        // Act
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 1);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.Same(expectedSymbol, calledSymbol);
    }

    [Fact]
    public void ListGenericOverload()
    {
        // foo(List<bool>());

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(GenericExpression(NameExpression("List"), NameType("bool")))));
        var compilation = CreateCompilation(tree);

        // Act
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 2);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.True(calledSymbol.IsGenericInstance);
        Assert.Same(expectedSymbol, calledSymbol.GenericDefinition);
    }

    [Fact]
    public void IEnumerableInt32Overload()
    {
        // foo(AsEnumerable(List<int32>()));

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(
                NameExpression("AsEnumerable"),
                CallExpression(GenericExpression(NameExpression("List"), NameType("int32"))))));
        var compilation = CreateCompilation(tree);

        // Act
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 3);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.Same(expectedSymbol, calledSymbol);
    }

    [Fact]
    public void IEnumerableStringOverload()
    {
        // foo(AsEnumerable(List<string>()));

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(
                NameExpression("AsEnumerable"),
                CallExpression(GenericExpression(NameExpression("List"), NameType("string"))))));
        var compilation = CreateCompilation(tree);

        // Act
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 4);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.Same(expectedSymbol, calledSymbol);
    }

    [Fact]
    public void IEnumerableGenericOverload()
    {
        // foo(AsEnumerable(List<bool>()));

        // Arrange
        var tree = CreateOverloadTree(CallExpression(
            NameExpression("foo"),
            CallExpression(
                NameExpression("AsEnumerable"),
                CallExpression(GenericExpression(NameExpression("List"), NameType("bool"))))));
        var compilation = CreateCompilation(tree);

        // Act
        var expectedSymbol = GetDeclaredFunctionSymbol(compilation, 5);
        var calledSymbol = GetCalledFunctionSymbol(compilation);

        // Assert
        Assert.Empty(compilation.Diagnostics);
        Assert.True(calledSymbol.IsGenericInstance);
        Assert.Same(expectedSymbol, calledSymbol.GenericDefinition);
    }
}
