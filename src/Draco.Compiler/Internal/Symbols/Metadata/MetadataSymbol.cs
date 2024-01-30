using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Api;
using Draco.Compiler.Internal.Symbols.Synthetized;
using Draco.Compiler.Internal.Utilities;

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

    public static Symbol ToSymbol(Symbol containingSymbol, TypeDefinition typeDefinition)
    {
        if (typeDefinition.Attributes.HasFlag(StaticClassAttributes))
        {
            // Static classes are treated as modules, nothing extra to do
            return new MetadataStaticClassSymbol(containingSymbol, typeDefinition);
        }
        else
        {
            // Non-static classes
            return new MetadataTypeSymbol(containingSymbol, typeDefinition);
        }
    }

    public static IEnumerable<Symbol> GetAdditionalSymbols(
        Symbol typeSymbol,
        TypeDefinition typeDefinition,
        MetadataReader metadataReader)
    {
        // Constructors
        if (!typeDefinition.Attributes.HasFlag(TypeAttributes.Abstract))
        {
            // Look for the constructors
            foreach (var methodHandle in typeDefinition.GetMethods())
            {
                var method = metadataReader.GetMethodDefinition(methodHandle);

                // Skip private
                if (method.Attributes.HasFlag(MethodAttributes.Private)) continue;

                // Match name
                var methodName = metadataReader.GetString(method.Name);
                if (methodName != ".ctor") continue;

                // This is a public constructor, synthetize a function overload
                var ctor = new MetadataMethodSymbol(typeSymbol, method);
                var ctorFunction = new ConstructorFunctionSymbol(ctor);
                yield return ctorFunction;
            }
        }

        // We look for operator symbols
        foreach (var methodHandle in typeDefinition.GetMethods())
        {
            var method = metadataReader.GetMethodDefinition(methodHandle);

            // Skip private
            if (method.Attributes.HasFlag(MethodAttributes.Private)) continue;

            // Skip non-specialname
            if (!method.Attributes.HasFlag(MethodAttributes.SpecialName)) continue;

            // Name must start with "op_"
            var methodName = metadataReader.GetString(method.Name);
            if (!methodName.StartsWith("op_")) continue;

            // This is an operator, synthetize a function overload
            var op = new MetadataMethodSymbol(typeSymbol, method);
            yield return op;
        }
    }

    public static string? GetDefaultMemberAttributeName(TypeDefinition typeDefinition, Compilation compilation, MetadataReader reader)
    {
        foreach (var attributeHandle in typeDefinition.GetCustomAttributes())
        {
            var attribute = reader.GetCustomAttribute(attributeHandle);
            var typeProvider = compilation.TypeProvider;
            switch (attribute.Constructor.Kind)
            {
            case HandleKind.MethodDefinition:
                var method = reader.GetMethodDefinition((MethodDefinitionHandle)attribute.Constructor);
                var methodType = reader.GetTypeDefinition(method.GetDeclaringType());
                if (reader.GetString(methodType.Name) == "DefaultMemberAttribute")
                {
                    return attribute.DecodeValue(typeProvider).FixedArguments[0].Value?.ToString();
                }
                break;
            case HandleKind.MemberReference:
                var member = reader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var memberType = reader.GetTypeReference((TypeReferenceHandle)member.Parent);
                if (reader.GetString(memberType.Name) == "DefaultMemberAttribute")
                {
                    return attribute.DecodeValue(typeProvider).FixedArguments[0].Value?.ToString();
                }
                break;
            default: throw new InvalidOperationException();
            };
        }
        return null;
    }

    public static object? DecodeConstant(Constant constant, MetadataReader metadataReader)
    {
        var blob = metadataReader.GetBlobBytes(constant.Value);
        return constant.TypeCode switch
        {
            ConstantTypeCode.NullReference => null,

            ConstantTypeCode.Boolean => BitConverter.ToBoolean(blob),
            ConstantTypeCode.Char => BitConverter.ToChar(blob),

            ConstantTypeCode.String => BitConverter.ToString(blob),

            ConstantTypeCode.Byte => blob[0],
            ConstantTypeCode.UInt16 => BitConverter.ToUInt16(blob),
            ConstantTypeCode.UInt32 => BitConverter.ToUInt32(blob),
            ConstantTypeCode.UInt64 => BitConverter.ToUInt64(blob),

            ConstantTypeCode.SByte => (sbyte)blob[0],
            ConstantTypeCode.Int16 => BitConverter.ToInt16(blob),
            ConstantTypeCode.Int32 => BitConverter.ToInt32(blob),
            ConstantTypeCode.Int64 => BitConverter.ToInt64(blob),

            ConstantTypeCode.Single => BitConverter.ToSingle(blob),
            ConstantTypeCode.Double => BitConverter.ToDouble(blob),

            _ => throw new ArgumentOutOfRangeException(nameof(constant)),
        };
    }

    /// <summary>
    /// Gets the documentation XML as text for the given <paramref name="symbol"/>.
    /// </summary>
    /// <param name="symbol">The <see cref="Symbol"/> to get documentation for.</param>
    /// <returns>The documentation, or empty string, if no documentation was found.</returns>
    public static string GetDocumentation(Symbol symbol)
    {
        var assembly = symbol.AncestorChain.OfType<MetadataAssemblySymbol>().FirstOrDefault();
        if (assembly is null) return string.Empty;
        var documentationName = GetPrefixedDocumentationName(symbol);
        var root = assembly.AssemblyDocumentation?.DocumentElement;
        var xml = root?.SelectSingleNode($"//member[@name='{documentationName}']")?.InnerXml ?? string.Empty;
        return string.Join(Environment.NewLine, xml.ReplaceLineEndings("\n").Split('\n').Select(x => x.TrimStart()));
    }

    /// <summary>
    /// Gets the full name of a <paramref name="symbol"/> used to retrieve documentation from metadata.
    /// </summary>
    /// <param name="symbol">The symbol to get documentation name of.</param>
    /// <returns>The documentation name, or empty string, if <paramref name="symbol"/> is null.</returns>
    public static string GetDocumentationName(Symbol? symbol) => symbol switch
    {
        FunctionSymbol function => GetFunctionDocumentationName(function),
        TypeParameterSymbol typeParam => GetTypeParameterDocumentationName(typeParam),
        null => string.Empty,
        _ => symbol.MetadataFullName,
    };

    /// <summary>
    /// The documentation name of <paramref name="symbol"/> with prepended documentation prefix, documentation prefix specifies the type of symbol the documentation name represents.
    /// For example <see cref="TypeSymbol"/> has the prefix "T:".
    /// </summary>
    /// <param name="symbol">The symbol to get prefixed documentation name of.</param>
    /// <returns>The prefixed documentation name, or empty string, if <paramref name="symbol"/> is null.</returns>
    public static string GetPrefixedDocumentationName(Symbol? symbol) =>
        $"{GetDocumentationPrefix(symbol)}{GetDocumentationName(symbol)}";

    private static string GetDocumentationPrefix(Symbol? symbol) => symbol switch
    {
        TypeSymbol => "T:",
        ModuleSymbol => "T:",
        FunctionSymbol => "M:",
        PropertySymbol => "P:",
        FieldSymbol => "F:",
        _ => string.Empty,
    };

    private static string GetFunctionDocumentationName(FunctionSymbol function)
    {
        var parametersJoined = function.Parameters.Length == 0
            ? string.Empty
            : $"({string.Join(",", function.Parameters.Select(x => GetDocumentationName(x.Type)))})";

        var generics = function.GenericParameters.Length == 0
            ? string.Empty
            : $"``{function.GenericParameters.Length}";
        return $"{function.MetadataFullName}{generics}{parametersJoined}";
    }

    private static string GetTypeParameterDocumentationName(TypeParameterSymbol typeParameter)
    {
        var index = typeParameter.ContainingSymbol?.GenericParameters.IndexOf(typeParameter);
        if (index is null || index.Value == -1) return typeParameter.MetadataFullName;
        return typeParameter.ContainingSymbol switch
        {
            TypeSymbol => $"`{index.Value}",
            FunctionSymbol => $"``{index.Value}",
            _ => typeParameter.MetadataFullName,
        };
    }
}
