using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Draco.Compiler.Internal.BoundTree;
using Draco.Compiler.Internal.Symbols.Generic;
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
        if (type.IsGenericDefinition)
        {
            // NOTE: Maybe this could be lazier
            // For now I went for correctness

            // Create a new set of generic parameters
            var genericParams = type.GenericParameters
                .Select(p => new KeyValuePair<TypeParameterSymbol, TypeSymbol>(
                    p,
                    new SynthetizedTypeParameterSymbol(p.Name)))
                .ToImmutableDictionary();
            var genericCtx = new GenericContext(genericParams);

            // Instantiate the type and ctor with these
            var instantiatedType = type.GenericInstantiate(type.ContainingSymbol, genericCtx);
            // NOTE: This is really janky...
            var ctorSymbol = new MetadataMethodSymbol(instantiatedType, ctorMethod) as FunctionSymbol;
            ctorSymbol = ctorSymbol.GenericInstantiate(instantiatedType, genericCtx);

            // TODO: This is very likely incorrect
            return new LazySynthetizedFunctionSymbol(
                name: type.Name,
                genericParametersBuilder: _ => genericParams.Values
                    .Cast<TypeParameterSymbol>()
                    .ToImmutableArray(),
                parametersBuilder: _ =>
                {
                    // Parameters
                    var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
                    foreach (var param in ctorSymbol.Parameters)
                    {
                        var paramSym = new SynthetizedParameterSymbol(param.Name, param.Type);
                        parameters.Add(paramSym);
                    }
                    return parameters.ToImmutableArray();
                },
                returnTypeBuilder: _ => instantiatedType,
                bodyBuilder: f => ExpressionStatement(ReturnExpression(
                    value: ObjectCreationExpression(
                        objectType: instantiatedType,
                        constructor: ctorSymbol,
                        arguments: f.Parameters
                            .Select(ParameterExpression)
                            .Cast<BoundExpression>()
                            .ToImmutableArray()))));
        }
        else
        {
            var ctorSymbol = new MetadataMethodSymbol(type, ctorMethod);
            return new LazySynthetizedFunctionSymbol(
                name: type.Name,
                genericParametersBuilder: _ => ImmutableArray<TypeParameterSymbol>.Empty,
                parametersBuilder: _ =>
                {
                    // Parameters
                    var parameters = ImmutableArray.CreateBuilder<ParameterSymbol>();
                    foreach (var param in ctorSymbol.Parameters)
                    {
                        var paramSym = new SynthetizedParameterSymbol(param.Name, param.Type);
                        parameters.Add(paramSym);
                    }
                    return parameters.ToImmutableArray();
                },
                returnTypeBuilder: _ => type,
                bodyBuilder: f => ExpressionStatement(ReturnExpression(
                    value: ObjectCreationExpression(
                        objectType: type,
                        constructor: ctorSymbol,
                        arguments: f.Parameters
                            .Select(ParameterExpression)
                            .Cast<BoundExpression>()
                            .ToImmutableArray()))));
        }
    }
}
