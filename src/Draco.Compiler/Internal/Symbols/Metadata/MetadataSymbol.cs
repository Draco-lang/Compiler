using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Synthetized;
using static Draco.Compiler.Internal.BoundTree.BoundTreeFactory;

namespace Draco.Compiler.Internal.Symbols.Metadata;

/// <summary>
/// Utilities for reading up metadata symbols.
/// </summary>
internal static class MetadataSymbol
{
    /// <summary>
    /// Attributes of a static class.
    /// </summary>
    private static readonly TypeAttributes StaticClassAttributes =
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

    private static FunctionSymbol SynthetizeConstructor(
        MetadataTypeSymbol type,
        MethodDefinition ctorMethod)
    {
        var ctorSymbol = new MetadataMethodSymbol(type, ctorMethod);

        return new LazySynthetizedFunctionSymbol(type.Name, () =>
        {
            // Parameters
            var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
            foreach (var param in ctorSymbol.Parameters)
            {
                var paramSym = new SynthetizedParameterSymbol(param.Name, param.Type);
                parameters.Add(paramSym);
            }

            // Build body
            var body = ExpressionStatement(ReturnExpression(
                value: ObjectCreationExpression(
                    objectType: type,
                    constructor: ctorSymbol,
                    arguments: parameters
                        .Select(ParameterExpression)
                        .Cast<BoundExpression>()
                        .ToImmutableArray())));

            // Done
            return (parameters.ToImmutable(), type, body);
        });
    }
}
