using Draco.Compiler.Internal.Symbols;
using System.Collections.Generic;
using System;
using System.Linq;

internal static class SymbolExtensions
{
    /// <summary>
    /// Retrieves all attributes of the given type attached to this symbol.
    /// </summary>
    /// <param name="this">The symbol to retrieve attributes from.</param>
    /// <param name="attributeType">The type of the attribute to retrieve.</param>
    /// <returns>The attributes of type <paramref name="attributeType"/> attached to this symbol.</returns>
    public static IEnumerable<AttributeInstance> GetAttributes(this Symbol @this, TypeSymbol attributeType) =>
        @this.Attributes.Where(attr => SymbolEqualityComparer.Default.Equals(attr.Constructor.ContainingSymbol, attributeType));

    /// <summary>
    /// Retrieves an attribute of the given type attached to this symbol.
    /// </summary>
    /// <param name="this">The symbol to retrieve attributes from.</param>
    /// <param name="attributeType">The type of the attribute to retrieve.</param>
    /// <returns>The attribute instance, if found, otherwise null.</returns>
    public static AttributeInstance? GetAttribute(this Symbol @this, TypeSymbol attributeType) =>
        @this.GetAttributes(attributeType).FirstOrDefault();

    /// <summary>
    /// Retrieves all attributes of the given type attached to this symbol, translated to the given attribute type.
    /// </summary>
    /// <typeparam name="T">The attribute type to translate to.</typeparam>
    /// <param name="this">The symbol to retrieve attributes from.</param>
    /// <param name="attributeType">The type of the attribute to retrieve.</param>
    /// <returns>The attributes of type <paramref name="attributeType"/> attached to this symbol.</returns>
    public static IEnumerable<T> GetAttributes<T>(this Symbol @this, TypeSymbol attributeType)
        where T : Attribute => @this
        .GetAttributes(attributeType)
        .Select(attr => attr.ToAttribute<T>());

    /// <summary>
    /// Retrieves an attribute of the given type attached to this symbol translated to the given attribute type.
    /// </summary>
    /// <typeparam name="T">The attribute type to translate to.</typeparam>
    /// <param name="this">The symbol to retrieve attributes from.</param>
    /// <param name="attributeType">The type of the attribute to retrieve.</param>
    /// <returns>The attribute instance, if found, otherwise null.</returns>
    public static T? GetAttribute<T>(this Symbol @this, TypeSymbol attributeType)
        where T : Attribute => @this.GetAttributes<T>(attributeType).FirstOrDefault();
}
