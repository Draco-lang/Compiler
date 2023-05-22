using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Synthetized;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utilities for reading up metadata symbols.
/// </summary>
internal static class MetadataSymbol
{
    /// <summary>
    /// Attributes of a static class.
    /// </summary>
    public static readonly TypeAttributes StaticClassAttributes =
        TypeAttributes.Abstract | TypeAttributes.Sealed | TypeAttributes.Class;

    public static IEnumerable<Symbol> ToSymbol(
        Symbol containingSymbol,
        TypeDefinition type,
        MetadataReader metadataReader)
    {
        if (type.Attributes.HasFlag(StaticClassAttributes))
        {
            // Static classes are treated as modules, nothing extra to do
            var result = new MetadataStaticClassSymbol(containingSymbol, type);
            return new[] { result };
        }
        else
        {
            // Non-static classes get constructor methods injected, in case they are not abstract
            var typeSymbol = new MetadataTypeSymbol(containingSymbol, type);
            var results = new List<Symbol>() { typeSymbol };
            if (!type.Attributes.HasFlag(TypeAttributes.Abstract))
            {
                // Look for the constructors
                foreach (var methodHandle in type.GetMethods())
                {
                    var method = metadataReader.GetMethodDefinition(methodHandle);
                    var methodName = metadataReader.GetString(method.Name);
                    if (methodName != ".ctor") continue;

                    // This is a constructor, synthetize a function overload
                    var ctor = SynthetizeConstructor(typeSymbol, method);
                    results.Add(ctor);
                }
            }
            return results;
        }
    }

    public static string? GetDefaultMemberAttributeName(TypeDefinition typeDefinition, Compilation compilation, MetadataReader reader)
    {
        foreach (var attributeHandle in typeDefinition.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(attributeHandle);
            var typeProvider = new TypeProvider(compilation!);
            switch (attribute.Constructor.Kind)
            {
            case HandleKind.MethodDefinition:
                var method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                var methodType = reader.GetTypeDefinition(method.GetDeclaringType());
                if (reader.GetString(methodType.Name) == "DefaultMemberAttribute") return attribute.DecodeValue(typeProvider).FixedArguments[0].Value?.ToString();
                break;
            case HandleKind.MemberReference:
                var member = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var memberType = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                if (reader.GetString(memberType.Name) == "DefaultMemberAttribute") return attribute.DecodeValue(typeProvider).FixedArguments[0].Value?.ToString();
                break;
            default: throw new System.InvalidOperationException();
            };
        }
        return null;
    }

    private static FunctionSymbol SynthetizeConstructor(
        MetadataTypeSymbol type,
        MethodDefinition ctorMethod) => new SynthetizedMetadataConstructorSymbol(type, ctorMethod);
}
