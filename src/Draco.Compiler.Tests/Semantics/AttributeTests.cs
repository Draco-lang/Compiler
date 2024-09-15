using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Draco.Compiler.Api.Syntax;
using Draco.Compiler.Internal;
using Draco.Compiler.Internal.Symbols;
using static Draco.Compiler.Api.Syntax.SyntaxFactory;
using static Draco.Compiler.Tests.TestUtilities;

namespace Draco.Compiler.Tests.Semantics;

public sealed class AttributeTests
{
    private static T AssertAttribute<T>(
        Symbol target,
        TypeSymbol attribType)
        where T : Attribute
    {
        var instance = target.GetAttribute(attribType);
        Assert.NotNull(instance);
        Assert.True(SymbolEqualityComparer.Default.Equals(attribType, instance.Type));

        var result = target.GetAttribute<T>(attribType);
        Assert.NotNull(result);
        return result;
    }

    [Fact]
    public void AttributesReadUpFromMetadata()
    {
        var csReference = CompileCSharpToMetadataReference("""
            using System;

            [Obsolete("do not use this class")]
            public class SomeClass
            {
                [Obsolete("do not use this field")]
                public int someField;

                [Obsolete("do not use this property")]
                public int SomeProperty { get; set; }

                [Obsolete("do not use this method")]
                public void SomeMethod() { }

                [Obsolete("do not use this constructor")]
                public SomeClass() { }
            }
            """);

        var syntax = SyntaxTree.Parse("""
            func main() {
                var c = SomeClass();
            }
            """);

        var compilation = CreateCompilation(syntax, additionalReferences: [csReference]);
        var semanticModel = compilation.GetSemanticModel(syntax);

        var varDeclSyntax = syntax.GetNode<VariableDeclarationSyntax>();
        var varDeclSymbol = GetInternalSymbol<LocalSymbol>(semanticModel.GetDeclaredSymbol(varDeclSyntax));
        var someClassSymbol = varDeclSymbol.Type;

        var callSyntax = syntax.GetNode<CallExpressionSyntax>();
        var callSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(callSyntax));

        var obsoleteAttrSymbol = compilation.RootModule
            .Lookup(["System", "ObsoleteAttribute"])
            .OfType<TypeSymbol>()
            .Single();

        Assert.Empty(compilation.Diagnostics);

        // Assert attribute on class
        var classAttr = AssertAttribute<ObsoleteAttribute>(someClassSymbol, obsoleteAttrSymbol);
        Assert.Equal("do not use this class", classAttr.Message);

        // Assert attribute on field
        var fieldSymbol = AssertMember<FieldSymbol>(someClassSymbol, "someField");
        var fieldAttr = AssertAttribute<ObsoleteAttribute>(fieldSymbol, obsoleteAttrSymbol);
        Assert.Equal("do not use this field", fieldAttr.Message);

        // Assert attribute on property
        var propertySymbol = AssertMember<PropertySymbol>(someClassSymbol, "SomeProperty");
        var propertyAttr = AssertAttribute<ObsoleteAttribute>(propertySymbol, obsoleteAttrSymbol);
        Assert.Equal("do not use this property", propertyAttr.Message);

        // Assert attribute on method
        var methodSymbol = AssertMember<FunctionSymbol>(someClassSymbol, "SomeMethod");
        var methodAttr = AssertAttribute<ObsoleteAttribute>(methodSymbol, obsoleteAttrSymbol);
        Assert.Equal("do not use this method", methodAttr.Message);

        // Assert that the constructor attribute was inherited by the constructor function
        var ctorAttr = AssertAttribute<ObsoleteAttribute>(callSymbol, obsoleteAttrSymbol);
        Assert.Equal("do not use this constructor", ctorAttr.Message);
    }

    [Fact]
    public void AttributeIsWrittenIntoMetadata()
    {
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            ImportDeclaration("System", "Diagnostics", "CodeAnalysis"),
            FunctionDeclaration(
                [Attribute(NameType("ObsoleteAttribute"), StringExpression("do not use this function"))],
                "foo",
                ParameterList(Parameter([Attribute(NameType("AllowNullAttribute"))], "arg", NameType("object"))),
                null,
                BlockFunctionBody())));

        var assembly = CompileToAssembly([tree]);

        var fooSymbol = assembly
            .GetType(CompilerConstants.DefaultModuleName)?
            .GetMethod("foo", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(fooSymbol);

        var obsoleteAttr = fooSymbol.GetCustomAttribute<ObsoleteAttribute>();
        Assert.NotNull(obsoleteAttr);
        Assert.Equal("do not use this function", obsoleteAttr.Message);

        var argParam = fooSymbol.GetParameters().Single();
        var allowNullAttr = argParam.GetCustomAttribute<AllowNullAttribute>();
        Assert.NotNull(allowNullAttr);
    }

    [Fact]
    public void AttributeReferencesTheSymbol()
    {
        var tree = SyntaxTree.Create(CompilationUnit(
            ImportDeclaration("System"),
            ImportDeclaration("System", "Diagnostics", "CodeAnalysis"),
            FunctionDeclaration(
                [Attribute(NameType("ObsoleteAttribute"), StringExpression("do not use this function"))],
                "foo",
                ParameterList(Parameter([Attribute(NameType("AllowNullAttribute"))], "arg", NameType("object"))),
                null,
                BlockFunctionBody())));

        var compilation = CreateCompilation(tree);
        var semanticModel = compilation.GetSemanticModel(tree);

        var obsoleteAttrSyntax = tree.GetNode<AttributeSyntax>();
        var obsoleteAttrSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(obsoleteAttrSyntax));

        var allowNullAttrSyntax = tree.GetNode<AttributeSyntax>(1);
        var allowNullAttrSymbol = GetInternalSymbol<FunctionSymbol>(semanticModel.GetReferencedSymbol(allowNullAttrSyntax));

        // The attribute syntax references the constructor

        Assert.NotNull(obsoleteAttrSymbol);
        Assert.False(obsoleteAttrSymbol.IsError);
        Assert.True(obsoleteAttrSymbol.IsConstructor);
        Assert.True(((TypeSymbol)obsoleteAttrSymbol.ContainingSymbol!).IsAttributeType);

        Assert.NotNull(allowNullAttrSymbol);
        Assert.False(allowNullAttrSymbol.IsError);
        Assert.True(allowNullAttrSymbol.IsConstructor);
        Assert.True(((TypeSymbol)allowNullAttrSymbol.ContainingSymbol!).IsAttributeType);
    }
}
